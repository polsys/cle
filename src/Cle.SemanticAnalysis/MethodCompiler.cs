using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Implements semantic analysis of method bodies.
    /// Before this pass, the declaration can be compiled <see cref="MethodDeclarationCompiler"/>.
    /// A single instance of this class may be reused for multiple method bodies.
    /// </summary>
    public class MethodCompiler : INameResolver
    {
        // These fields never change during the lifetime of the instance
        private readonly IDiagnosticSink _diagnostics;
        private readonly IDeclarationProvider _declarationProvider;

        // These fields hold information for the current method and are reset by CompileBody()
        private FunctionSyntax? _syntaxTree;
        private MethodDeclaration? _declaration;
        private string? _definingNamespace;
        private string? _sourceFilename;
        private readonly ScopedVariableMap _variableMap;

        // These fields are reset by InternalCompile()
        private CompiledMethod? _methodInProgress;

        /// <summary>
        /// Creates an instance that can be used for method body compilation.
        /// The instance can be reused for successive <see cref="CompileBody"/> calls.
        /// </summary>
        /// <param name="declarationProvider">The provider for method and type declarations.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        public MethodCompiler(
            IDeclarationProvider declarationProvider,
            IDiagnosticSink diagnosticSink)
        {
            _declarationProvider = declarationProvider;
            _diagnostics = diagnosticSink;
            _variableMap = new ScopedVariableMap();
        }

        /// <summary>
        /// Performs semantic analysis on the given method syntax tree and returns the compiled method if successful.
        /// Returns null if the compilation fails, in which case diagnostics are also emitted.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree for the method.</param>
        /// <param name="declaration">The method declaration.</param>
        /// <param name="definingNamespace">The namespace where the method is defined.</param>
        /// <param name="sourceFilename">The name of the file where the method is defined.</param>
        public CompiledMethod? CompileBody(
            FunctionSyntax syntaxTree,
            MethodDeclaration declaration,
            string definingNamespace,
            string sourceFilename)
        {
            // Reset most per-method fields
            _syntaxTree = syntaxTree;
            _declaration = declaration;
            _definingNamespace = definingNamespace;
            _sourceFilename = sourceFilename;
            _variableMap.Reset();

            return InternalCompile();
        }

        private CompiledMethod? InternalCompile()
        {
            Debug.Assert(_syntaxTree != null);
            Debug.Assert(_syntaxTree.Block != null);
            Debug.Assert(_sourceFilename != null);
            Debug.Assert(_declaration != null);

            _methodInProgress = new CompiledMethod(_declaration.FullName);

            // Create locals for the parameters
            _variableMap.PushScope();
            for (var i = 0; i < _declaration.ParameterTypes.Count; i++)
            {
                var paramSyntax = _syntaxTree.Parameters[i];
                var paramIndex = _methodInProgress.AddLocal(_declaration.ParameterTypes[i], LocalFlags.Parameter);

                if (!_variableMap.TryAddVariable(paramSyntax.Name, paramIndex))
                {
                    _diagnostics.Add(DiagnosticCode.VariableAlreadyDefined, paramSyntax.Position, paramSyntax.Name);
                    return null;
                }
            }

            // Compile the body
            var graphBuilder = new BasicBlockGraphBuilder();
            if (!TryCompileBlock(_syntaxTree.Block, graphBuilder.GetInitialBlockBuilder(),
                out var finalBlockBuilder, out var returnGuaranteed))
            {
                return null;
            }

            // ReSharper nullability checker cannot see that the fields weren't changed in TryCompileBlock
            Debug.Assert(_declaration != null);
            Debug.Assert(_methodInProgress != null);

            if (!returnGuaranteed)
            {
                // Void methods may return implicitly
                // Others should fail
                if (_declaration.ReturnType.Equals(SimpleType.Void))
                {
                    var voidIndex = _methodInProgress.AddLocal(SimpleType.Void, LocalFlags.None);
                    finalBlockBuilder.AppendInstruction(Opcode.Return, voidIndex, 0, 0);
                }
                else
                {
                    Debug.Assert(_syntaxTree != null);
                    _diagnostics.Add(DiagnosticCode.ReturnNotGuaranteed, _syntaxTree.Position, _syntaxTree.Name);
                    return null;
                }
            }

            _methodInProgress.Body = graphBuilder.Build();
            return _methodInProgress;
        }

        private bool TryCompileBlock(BlockSyntax block, BasicBlockBuilder builder,
            out BasicBlockBuilder newBuilder, out bool returnGuaranteed)
        {
            // Some statements may create new basic blocks, but the default is that they do not
            newBuilder = builder;
            returnGuaranteed = false;
            var deadCodeWarningEmitted = false;

            // Create a new variable scope
            _variableMap.PushScope();

            foreach (var statement in block.Statements)
            {
                // Emit a dead code warning if return is already guaranteed
                if (returnGuaranteed && !deadCodeWarningEmitted)
                {
                    _diagnostics.Add(DiagnosticCode.UnreachableCode, statement.Position);
                    deadCodeWarningEmitted = true;
                }

                switch (statement)
                {
                    case AssignmentSyntax assignment:
                        if (!TryCompileAssignment(assignment, builder))
                            return false;
                        break;
                    case BlockSyntax innerBlock:
                        if (!TryCompileBlock(innerBlock, builder, out newBuilder, out var blockReturns))
                            return false;
                        builder = newBuilder;
                        returnGuaranteed |= blockReturns;
                        break;
                    case FunctionCallStatementSyntax call:
                        // Call statements are call expressions with the result ignored
                        Debug.Assert(_methodInProgress != null);
                        if (ExpressionCompiler.TryCompileExpression(call.Call, null, _methodInProgress, builder,
                            this, _diagnostics) == -1)
                        {
                            return false;
                        }
                        break;
                    case IfStatementSyntax ifStatement:
                        if (!TryCompileIf(ifStatement, builder, out newBuilder, out var ifReturns))
                            return false;
                        builder = newBuilder;
                        returnGuaranteed |= ifReturns;
                        break;
                    case ReturnStatementSyntax returnSyntax:
                        if (!TryCompileReturn(returnSyntax, builder))
                            return false;
                        returnGuaranteed = true;
                        break;
                    case VariableDeclarationSyntax variableDeclaration:
                        if (!TryCompileVariableDeclaration(variableDeclaration, builder))
                            return false;
                        break;
                    case WhileStatementSyntax whileStatement:
                        if (!TryCompileWhile(whileStatement, builder, out newBuilder))
                            return false;
                        builder = newBuilder;
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented statement type");
                }
            }

            // Destroy the variable scope
            // TODO: Destroy locals (both variables and temporaries!) defined with destructor
            _variableMap.PopScope();

            return true;
        }

        private bool TryCompileAssignment(AssignmentSyntax assignment, BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);

            // Get the target variable
            if (!_variableMap.TryGetVariable(assignment.Variable.Name, out var targetIndex))
            {
                _diagnostics.Add(DiagnosticCode.VariableNotFound, assignment.Variable.Position, assignment.Variable.Name);
                return false;
            }
            var expectedType = _methodInProgress.Values[targetIndex].Type;

            // Compile the expression
            var sourceIndex = ExpressionCompiler.TryCompileExpression(assignment.Value,
                expectedType, _methodInProgress, builder, this, _diagnostics);

            if (sourceIndex == -1)
                return false;

            // Emit a copy operation
            builder.AppendInstruction(Opcode.CopyValue, (ushort)sourceIndex, 0, (ushort)targetIndex);
            return true;
        }

        private bool TryCompileIf(IfStatementSyntax ifSyntax, BasicBlockBuilder builder,
            out BasicBlockBuilder newBuilder, out bool returnGuaranteed)
        {
            newBuilder = builder; // Default failure case
            returnGuaranteed = false;
            Debug.Assert(_methodInProgress != null);

            // Compile the condition expression
            var conditionValue = ExpressionCompiler.TryCompileExpression(ifSyntax.ConditionSyntax,
                SimpleType.Bool, _methodInProgress, builder, this, _diagnostics);
            if (conditionValue == -1)
            {
                return false;
            }

            // Compile the 'then' branch
            var thenBuilder = builder.CreateBranch((ushort)conditionValue);
            if (!TryCompileBlock(ifSyntax.ThenBlockSyntax, thenBuilder, out thenBuilder, out var thenReturns))
            {
                return false;
            }

            // Compile the possible 'else' branch.
            // It can be either an IfStatementSyntax (else if), BlockSyntax (else) or null (no else).
            if (ifSyntax.ElseSyntax is null)
            {
                // The block where the control flow merges back must not contain a critical edge, where
                // a branch leads to a block with several predecessors. If the 'then' block returns, this
                // won't happen; otherwise we must create a block for the negative branch.
                // This is required for the SSA form because PHIs in the merger block must be resolved
                // at the predecessors.
                if (thenReturns)
                {
                    // Happy case: the 'then' branch does not merge back
                    newBuilder = builder.CreateSuccessorBlock();
                }
                else
                {
                    // This block will contain PHI resolution for the pre-if values if newBuilder contains PHIs
                    var negativeLandingBlock = builder.CreateSuccessorBlock();

                    // This block is the real merger block
                    newBuilder = negativeLandingBlock.CreateSuccessorBlock();
                    thenBuilder.SetSuccessor(newBuilder.Index);
                }

                // Here, we cannot give a return guarantee unless the 'then' branch is always taken and returns
                // TODO: Return guarantee for const conditions
                return true;
            }
            else if (ifSyntax.ElseSyntax is BlockSyntax elseBlock)
            {
                // Compile the else block
                var elseBuilder = builder.CreateSuccessorBlock();
                if (!TryCompileBlock(elseBlock, elseBuilder, out elseBuilder, out var elseReturns))
                {
                    return false;
                }

                // Then create a new block that is the target of both the 'then' and 'else' blocks
                newBuilder = elseBuilder.CreateSuccessorBlock();
                thenBuilder.SetSuccessor(newBuilder.Index);
                returnGuaranteed = thenReturns & elseReturns;
                return true;
            }
            else if (ifSyntax.ElseSyntax is IfStatementSyntax elseIf)
            {
                // Compile the if, then make the 'then' block point to the created successor
                if (!TryCompileIf(elseIf, builder.CreateSuccessorBlock(), out newBuilder, out var elseIfReturns))
                {
                    return false;
                }

                thenBuilder.SetSuccessor(newBuilder.Index);
                returnGuaranteed = thenReturns & elseIfReturns;
                return true;
            }
            else
            {
                throw new InvalidOperationException("Invalid syntax node type for 'else'.");
            }
        }

        private bool TryCompileWhile(WhileStatementSyntax whileSyntax, BasicBlockBuilder builder,
            out BasicBlockBuilder newBuilder)
        {
            newBuilder = builder; // Default failure case
            Debug.Assert(_methodInProgress != null);

            // Create a new basic block which will be the backwards branch target
            var conditionBuilder = builder.CreateSuccessorBlock();
            var conditionBlockIndex = conditionBuilder.Index;

            // Compile the condition
            var conditionValue = ExpressionCompiler.TryCompileExpression(whileSyntax.ConditionSyntax,
                SimpleType.Bool, _methodInProgress, conditionBuilder, this, _diagnostics);
            if (conditionValue == -1)
            {
                return false;
            }

            // Then compile the body.
            // The return guarantee is not propagated up as we don't know whether the loop will ever be entered.
            // TODO: Recognizing compile-time constant condition
            var bodyBuilder = conditionBuilder.CreateBranch((ushort)conditionValue);
            if (!TryCompileBlock(whileSyntax.BodySyntax, bodyBuilder, out bodyBuilder, out var _))
            {
                return false;
            }

            // Create the backwards branch
            bodyBuilder.SetSuccessor(conditionBlockIndex);

            // Create the exit branch
            newBuilder = conditionBuilder.CreateSuccessorBlock();

            return true;
        }

        private bool TryCompileReturn(ReturnStatementSyntax syntax, BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);
            Debug.Assert(_declaration != null);

            var returnValueNumber = -1; // ExpressionCompiler returns -1 on failure
            if (syntax.ResultExpression is null)
            {
                // Void return: verify that the method really returns void, then add a void local to return
                if (_declaration.ReturnType.Equals(SimpleType.Void))
                {
                    returnValueNumber = _methodInProgress.AddLocal(SimpleType.Void, LocalFlags.None);
                }
                else
                {
                    _diagnostics.Add(DiagnosticCode.TypeMismatch, syntax.Position, 
                        SimpleType.Void.TypeName, _declaration.ReturnType.TypeName);
                }
            }
            else
            {
                // Non-void return: parse the expression, verifying the type
                returnValueNumber = ExpressionCompiler.TryCompileExpression(syntax.ResultExpression,
                    _declaration.ReturnType, _methodInProgress, builder, this, _diagnostics);
            }
            
            // At this point, a diagnostic should already be logged
            if (returnValueNumber == -1)
                return false;

            builder.AppendInstruction(Opcode.Return, (ushort)returnValueNumber, 0, 0);
            return true;
        }

        private bool TryCompileVariableDeclaration(VariableDeclarationSyntax declaration,
            BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);
            var localCount = _methodInProgress.Values.Count;

            // Resolve the type and verify that it is not void
            if (!TypeResolver.TryResolve(declaration.Type, _diagnostics, declaration.Position, out var type))
            {
                return false;
            }
            Debug.Assert(type != null);
            if (type.Equals(SimpleType.Void))
            {
                _diagnostics.Add(DiagnosticCode.VoidIsNotValidType, declaration.Position, declaration.Name);
                return false;
            }

            // Compile the initial value (either a runtime expression or compile-time constant)
            var localIndex = ExpressionCompiler.TryCompileExpression(declaration.InitialValueExpression, 
                type, _methodInProgress, builder, this, _diagnostics);

            if (localIndex == -1)
                return false;

            // There are two cases:
            //   - ExpressionCompiler created temporaries, the last of which is our initialized variable,
            //   - ExpressionCompiler returned a reference to an existent local ("Type value = anotherVariable" only).
            // In the latter case, we must create a new local and emit a copy.
            if (localCount == _methodInProgress.Values.Count)
            {
                var source = localIndex;

                // Don't bother figuring out a correct initial value, it won't be used anyways
                localIndex = _methodInProgress.AddLocal(type, LocalFlags.None);
                builder.AppendInstruction(Opcode.CopyValue, (ushort)source, 0, (ushort)localIndex);
            }

            // Add it to the variable map (unless the name is already in use)
            if (!_variableMap.TryAddVariable(declaration.Name, localIndex))
            {
                _diagnostics.Add(DiagnosticCode.VariableAlreadyDefined, declaration.Position, declaration.Name);
                return false;
            }

            return true;
        }

        //
        // Name resolution for ExpressionCompiler
        //

        IReadOnlyList<MethodDeclaration> INameResolver.ResolveMethod(string name)
        {
            Debug.Assert(_sourceFilename != null);
            Debug.Assert(_definingNamespace != null);

            var separatorPos = name.LastIndexOf("::", StringComparison.InvariantCulture);
            if (separatorPos == -1)
            {
                // This is a simple name - search in all visible namespaces
                // TODO: Support for multiple visible namespaces
                return _declarationProvider.GetMethodDeclarations(name, new[] { _definingNamespace }, _sourceFilename);
            }
            else
            {
                // This is a full name - search only in the specified namespace
                var namespaceName = name.Substring(0, separatorPos);
                var simpleName = name.Substring(separatorPos + 2);

                return _declarationProvider.GetMethodDeclarations(simpleName, new[] { namespaceName }, _sourceFilename);
            }
        }

        bool INameResolver.TryResolveVariable(string name, out int localIndex)
        {
            return _variableMap.TryGetVariable(name, out localIndex);
        }
    }
}
