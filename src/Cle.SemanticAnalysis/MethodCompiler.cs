using System;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Implements semantic analysis of methods.
    /// </summary>
    public class MethodCompiler
    {
        // TODO: Implement pooling for repeated use.
        // TODO: Ideally, a single instance would be created per compiler thread and reset for each method.

        [NotNull] private readonly IDiagnosticSink _diagnostics;

        [NotNull] private readonly FunctionSyntax _syntaxTree;

        /// <summary>
        /// Verifies and creates type information for the method.
        /// Returns null if this fails, in which case diagnostics are also emitted.
        /// </summary>
        /// <param name="syntax">The syntax tree for the method.</param>
        /// <param name="declarationProvider">The type provider to use for resolving custom types.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        [CanBeNull]
        public static MethodDeclaration CompileDeclaration(
            [NotNull] FunctionSyntax syntax,
            [NotNull] IDeclarationProvider declarationProvider,
            [NotNull] IDiagnosticSink diagnosticSink)
        {
            // TODO: Proper type resolution with the declaration provider
            if (syntax.ReturnTypeName == "bool")
            {
                return new MethodDeclaration(SimpleType.Bool);
            }
            else if (syntax.ReturnTypeName == "int32")
            {
                return new MethodDeclaration(SimpleType.Int32);
            }
            else
            {
                diagnosticSink.Add(DiagnosticCode.TypeNotFound, syntax.Position, syntax.ReturnTypeName);
                return null;
            }

            // TODO: Resolve parameter types
            // TODO: Verify that the name does not already exist
        }

        /// <summary>
        /// Performs semantic analysis on the given method syntax tree and returns the compiled method if successful.
        /// Returns null if the compilation fails, in which case diagnostics are also emitted.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree for the method.</param>
        /// <param name="diagnosticSink">The receiver for any semantic errors or warnings.</param>
        [CanBeNull]
        public static CompiledMethod CompileBody(
            [NotNull] FunctionSyntax syntaxTree,
            [NotNull] IDiagnosticSink diagnosticSink)
        {
            var compiler = new MethodCompiler(syntaxTree, diagnosticSink);
            return compiler.InternalCompile();
        }

        private MethodCompiler([NotNull] FunctionSyntax syntaxTree, [NotNull] IDiagnosticSink diagnosticSink)
        {
            _syntaxTree = syntaxTree;
            _diagnostics = diagnosticSink;
        }

        [CanBeNull]
        private CompiledMethod InternalCompile()
        {
            // Fetch the param and return type information and create a working method instance

            // TODO: Create locals for the parameters

            // Compile the body

            // Assert that the method really returns
            
            throw new NotImplementedException();
        }
    }
}
