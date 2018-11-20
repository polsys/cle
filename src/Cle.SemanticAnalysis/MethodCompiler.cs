using System;
using System.Diagnostics;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
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
        [CanBeNull] private string _definingNamespace;
        [CanBeNull] private string _sourceFilename;

        // These fields are reset by InternalCompile()
        [CanBeNull] private MethodDeclaration _declaration;
        [CanBeNull] private CompiledMethod _methodInProgress;

        /// <summary>
        /// Verifies and creates type information for the method.
        /// Returns null if this fails, in which case diagnostics are also emitted.
        /// The name is not checked for duplication in this method.
        /// </summary>
        /// <param name="syntax">The syntax tree for the method.</param>
        /// <param name="definingFilename">The name of the file that contains the method.</param>
        /// <param name="declarationProvider">The type provider to use for resolving custom types.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        [CanBeNull]
        public static MethodDeclaration CompileDeclaration(
            [NotNull] FunctionSyntax syntax,
            [NotNull] string definingFilename,
            [NotNull] IDeclarationProvider declarationProvider,
            [NotNull] IDiagnosticSink diagnosticSink)
        {
            // TODO: Proper type resolution with the declaration provider
            if (syntax.ReturnTypeName == "bool")
            {
                return new MethodDeclaration(SimpleType.Bool, syntax.Visibility, definingFilename, syntax.Position);
            }
            else if (syntax.ReturnTypeName == "int32")
            {
                return new MethodDeclaration(SimpleType.Int32, syntax.Visibility, definingFilename, syntax.Position);
            }
            else
            {
                diagnosticSink.Add(DiagnosticCode.TypeNotFound, syntax.Position, syntax.ReturnTypeName);
                return null;
            }

            // TODO: Resolve parameter types
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
        }

        /// <summary>
        /// Performs semantic analysis on the given method syntax tree and returns the compiled method if successful.
        /// Returns null if the compilation fails, in which case diagnostics are also emitted.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree for the method.</param>
        /// <param name="definingNamespace">The namespace where the method is defined.</param>
        /// <param name="sourceFilename">The name of the file where the method is defined.</param>
        [CanBeNull]
        public CompiledMethod CompileBody(
            [NotNull] FunctionSyntax syntaxTree,
            [NotNull] string definingNamespace,
            [NotNull] string sourceFilename)
        {
            // Reset most per-method fields
            _syntaxTree = syntaxTree;
            _definingNamespace = definingNamespace;
            _sourceFilename = sourceFilename;
            
            return InternalCompile();
        }

        [CanBeNull]
        private CompiledMethod InternalCompile()
        {
            Debug.Assert(_syntaxTree != null);
            Debug.Assert(_sourceFilename != null);

            // Fetch the param and return type information and create a working method instance
            var possibleDeclarations = _declarationProvider.GetMethodDeclarations(_syntaxTree.Name,
                new[] { _definingNamespace }, _sourceFilename);
            if (possibleDeclarations.Count != 1)
                throw new InvalidOperationException("Ambiguous method declaration");
            _declaration = possibleDeclarations[0];

            _methodInProgress = new CompiledMethod();

            // TODO: Create locals for the parameters

            // Compile the body
            var graphBuilder = new BasicBlockGraphBuilder();
            TryCompileBlock(_syntaxTree.Block, graphBuilder.GetInitialBlockBuilder());

            // TODO: Assert that the method really returns

            // ReSharper disable once PossibleNullReferenceException
            _methodInProgress.Body = graphBuilder.Build();
            return _methodInProgress;
        }

        private bool TryCompileBlock([NotNull] BlockSyntax block, [NotNull] BasicBlockBuilder builder)
        {
            foreach (var statement in block.Statements)
            {
                switch (statement)
                {
                    case ReturnStatementSyntax returnSyntax:
                        TryCompileReturn(returnSyntax, builder);
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented statement type");
                }
            }

            return true;
        }

        private bool TryCompileReturn([NotNull] ReturnStatementSyntax syntax, [NotNull] BasicBlockBuilder builder)
        {
            // TODO: This throws for anything other than valid boolean literals
            // TODO: Implement proper expression compilation
            // TODO: Implement return type checking
            var returnValueNumber = _methodInProgress.AddTemporary(SimpleType.Bool,
                ConstantValue.Bool(((BooleanLiteralSyntax)syntax.ResultExpression).Value));

            builder.AppendInstruction(Opcode.Return, returnValueNumber, 0, 0);
            return true;
        }
    }
}
