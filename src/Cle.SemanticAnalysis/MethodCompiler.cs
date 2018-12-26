using System;
using System.Diagnostics;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Implements semantic analysis of methods.
    /// In the first pass, declarations can be compiled using the static <see cref="CompileDeclaration"/> method.
    /// Then, method bodies can be compiled using a reusable instance.
    /// </summary>
    public class MethodCompiler
    {
        // These fields never change during the lifetime of the instance
        [NotNull] private readonly IDiagnosticSink _diagnostics;
        [NotNull] private readonly IDeclarationProvider _declarationProvider;

        // These fields hold information for the current method and are reset by CompileBody()
        [CanBeNull] private FunctionSyntax _syntaxTree;
        [CanBeNull] private MethodDeclaration _declaration;
        [CanBeNull] private string _definingNamespace;
        [CanBeNull] private string _sourceFilename;
        [NotNull] private readonly ScopedVariableMap _variableMap;

        // These fields are reset by InternalCompile()
        [CanBeNull] private CompiledMethod _methodInProgress;

        /// <summary>
        /// Verifies and creates type information for the method.
        /// Returns null if this fails, in which case diagnostics are also emitted.
        /// The name is not checked for duplication in this method.
        /// </summary>
        /// <param name="syntax">The syntax tree for the method.</param>
        /// <param name="definingFilename">The name of the file that contains the method.</param>
        /// <param name="methodBodyIndex">The index associated with the compiled method body.</param>
        /// <param name="declarationProvider">The type provider to use for resolving custom types.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        [CanBeNull]
        public static MethodDeclaration CompileDeclaration(
            [NotNull] FunctionSyntax syntax,
            [NotNull] string definingFilename,
            int methodBodyIndex,
            [NotNull] IDeclarationProvider declarationProvider,
            [NotNull] IDiagnosticSink diagnosticSink)
        {
            // Resolve the return type
            if (!TryResolveType(syntax.ReturnTypeName, diagnosticSink, syntax.Position, out var returnType))
            {
                return null;
            }
            Debug.Assert(returnType != null);

            // TODO: Resolve parameter types

            // Apply the attributes
            var isEntryPoint = false;
            foreach (var attribute in syntax.Attributes)
            {
                if (attribute.Name == "EntryPoint")
                {
                    // TODO: Check that the method has no parameters
                    if (!returnType.Equals(SimpleType.Int32))
                    {
                        diagnosticSink.Add(DiagnosticCode.EntryPointMustBeDeclaredCorrectly, syntax.Position);
                        return null;
                    }

                    isEntryPoint = true;
                }
                else
                {
                    diagnosticSink.Add(DiagnosticCode.UnknownAttribute, attribute.Position, attribute.Name);
                    return null;
                }
            }

            return new MethodDeclaration(methodBodyIndex, returnType, syntax.Visibility,
                definingFilename, syntax.Position, isEntryPoint);
        }

        /// <summary>
        /// Creates an instance that can be used for method body compilation.
        /// The instance can be reused for successive <see cref="CompileBody"/> calls.
        /// </summary>
        /// <param name="declarationProvider">The provider for method and type declarations.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        public MethodCompiler(
            [NotNull] IDeclarationProvider declarationProvider,
            [NotNull] IDiagnosticSink diagnosticSink)
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
        [CanBeNull]
        public CompiledMethod CompileBody(
            [NotNull] FunctionSyntax syntaxTree,
            [NotNull] MethodDeclaration declaration,
            [NotNull] string definingNamespace,
            [NotNull] string sourceFilename)
        {
            // Reset most per-method fields
            _syntaxTree = syntaxTree;
            _declaration = declaration;
            _definingNamespace = definingNamespace;
            _sourceFilename = sourceFilename;
            
            // Variable map does not need to be reset if scope pushes/pops are balanced.

            return InternalCompile();
        }

        [CanBeNull]
        private CompiledMethod InternalCompile()
        {
            Debug.Assert(_syntaxTree != null);
            Debug.Assert(_sourceFilename != null);
            
            _methodInProgress = new CompiledMethod();

            // TODO: Create locals for the parameters

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
                    var voidIndex = _methodInProgress.AddLocal(SimpleType.Void, ConstantValue.Void());
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

        private bool TryCompileBlock([NotNull] BlockSyntax block, [NotNull] BasicBlockBuilder builder,
            [NotNull] out BasicBlockBuilder newBuilder, out bool returnGuaranteed)
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

        private bool TryCompileAssignment([NotNull] AssignmentSyntax assignment, [NotNull] BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);

            // Get the target variable
            if (!_variableMap.TryGetVariable(assignment.Variable, out var targetIndex))
            {
                _diagnostics.Add(DiagnosticCode.VariableNotFound, assignment.Position, assignment.Variable);
                return false;
            }
            var expectedType = _methodInProgress.Values[targetIndex].Type;

            // Compile the expression
            var sourceIndex = ExpressionCompiler.TryCompileExpression(assignment.Value,
                expectedType, _methodInProgress, builder, _variableMap, _diagnostics);

            if (sourceIndex == -1)
                return false;

            // Emit a copy operation
            builder.AppendInstruction(Opcode.CopyValue, sourceIndex, 0, targetIndex);
            return true;
        }

        private bool TryCompileIf([NotNull] IfStatementSyntax ifSyntax, [NotNull] BasicBlockBuilder builder,
            [NotNull] out BasicBlockBuilder newBuilder, out bool returnGuaranteed)
        {
            newBuilder = builder; // Default failure case
            returnGuaranteed = false;
            Debug.Assert(_methodInProgress != null);

            // Compile the condition expression
            var conditionValue = ExpressionCompiler.TryCompileExpression(ifSyntax.ConditionSyntax,
                SimpleType.Bool, _methodInProgress, builder, _variableMap, _diagnostics);
            if (conditionValue == -1)
            {
                return false;
            }

            // Compile the 'then' branch
            var thenBuilder = builder.CreateBranch(conditionValue);
            if (!TryCompileBlock(ifSyntax.ThenBlockSyntax, thenBuilder, out thenBuilder, out var thenReturns))
            {
                return false;
            }

            // Compile the possible 'else' branch.
            // It can be either an IfStatementSyntax (else if), BlockSyntax (else) or null (no else).
            if (ifSyntax.ElseSyntax is null)
            {
                newBuilder = builder.CreateSuccessorBlock();
                thenBuilder.SetSuccessor(newBuilder.Index);

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

        private bool TryCompileWhile([NotNull] WhileStatementSyntax whileSyntax, [NotNull] BasicBlockBuilder builder,
            [NotNull] out BasicBlockBuilder newBuilder)
        {
            newBuilder = builder; // Default failure case
            Debug.Assert(_methodInProgress != null);

            // Create a new basic block which will be the backwards branch target
            var conditionBuilder = builder.CreateSuccessorBlock();
            var conditionBlockIndex = conditionBuilder.Index;

            // Compile the condition
            var conditionValue = ExpressionCompiler.TryCompileExpression(whileSyntax.ConditionSyntax,
                SimpleType.Bool, _methodInProgress, conditionBuilder, _variableMap, _diagnostics);
            if (conditionValue == -1)
            {
                return false;
            }

            // Then compile the body.
            // The return guarantee is not propagated up as we don't know whether the loop will ever be entered.
            // TODO: Recognizing compile-time constant condition
            var bodyBuilder = conditionBuilder.CreateBranch(conditionValue);
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

        private bool TryCompileReturn([NotNull] ReturnStatementSyntax syntax, [NotNull] BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);
            Debug.Assert(_declaration != null);

            var returnValueNumber = -1; // ExpressionCompiler returns -1 on failure
            if (syntax.ResultExpression is null)
            {
                // Void return: verify that the method really returns void, then add a void local to return
                if (_declaration.ReturnType.Equals(SimpleType.Void))
                {
                    returnValueNumber = _methodInProgress.AddLocal(SimpleType.Void, ConstantValue.Void());
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
                    _declaration.ReturnType, _methodInProgress, builder, _variableMap, _diagnostics);
            }
            
            // At this point, a diagnostic should already be logged
            if (returnValueNumber == -1)
                return false;

            builder.AppendInstruction(Opcode.Return, returnValueNumber, 0, 0);
            return true;
        }

        private bool TryCompileVariableDeclaration([NotNull] VariableDeclarationSyntax declaration,
            [NotNull] BasicBlockBuilder builder)
        {
            Debug.Assert(_methodInProgress != null);
            var localCount = _methodInProgress.Values.Count;

            // Compile the initial value (either a runtime expression or compile-time constant)
            if (!TryResolveType(declaration.TypeName, _diagnostics, declaration.Position, out var type))
            {
                return false;
            }
            Debug.Assert(type != null);
            var localIndex = ExpressionCompiler.TryCompileExpression(declaration.InitialValueExpression, 
                type, _methodInProgress, builder, _variableMap, _diagnostics);

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
                localIndex = _methodInProgress.AddLocal(type, ConstantValue.Void());
                builder.AppendInstruction(Opcode.CopyValue, source, 0, localIndex);
            }

            // Add it to the variable map (unless the name is already in use)
            if (!_variableMap.TryAddVariable(declaration.Name, localIndex))
            {
                _diagnostics.Add(DiagnosticCode.VariableAlreadyDefined, declaration.Position, declaration.Name);
                return false;
            }

            return true;
        }
        
        private static bool TryResolveType(
            [NotNull] string typeName, 
            [NotNull] IDiagnosticSink diagnostics,
            TextPosition position,
            [CanBeNull] out TypeDefinition type)
        {
            // TODO: Proper type resolution with the declaration provider
            switch (typeName)
            {
                case "bool":
                    type = SimpleType.Bool;
                    break;
                case "int32":
                    type = SimpleType.Int32;
                    break;
                case "void":
                    type = SimpleType.Void;
                    break;
                default:
                    diagnostics.Add(DiagnosticCode.TypeNotFound, position, typeName);
                    type = null;
                    return false;
            }

            return true;
        }
    }
}
