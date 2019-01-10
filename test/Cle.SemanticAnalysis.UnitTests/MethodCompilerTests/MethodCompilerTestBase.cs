using System;
using System.Text;
using Cle.Parser;
using Cle.SemanticAnalysis.IR;
using Cle.UnitTests.Common;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class MethodCompilerTestBase
    {
        /// <summary>
        /// Parses the given source code, compiles the method declaration and
        /// returns the result of <see cref="MethodCompiler.CompileBody"/>.
        /// The source code must contain exactly one global method.
        /// </summary>
        protected CompiledMethod TryCompileSingleMethod([NotNull] string source, [NotNull] out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            const string sourceFilename = "test.cle";
            var syntaxTree = SyntaxParser.Parse(sourceBytes.AsMemory(), sourceFilename, diagnostics);
            Assert.That(syntaxTree, Is.Not.Null, "Source file was not parsed successfully.");
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items, "Expected only a single method.");

            // If needed, refactor this to accept a custom declaration provider
            var declarationProvider = new TestingSingleFileDeclarationProvider();
            var declaration = MethodCompiler.CompileDeclaration(syntaxTree.Functions[0],
                syntaxTree.Namespace, sourceFilename, 0, declarationProvider, diagnostics);
            Assert.That(declaration, Is.Not.Null, "Method declaration was not compiled successfully.");
            declarationProvider.Methods.Add(syntaxTree.Functions[0].Name, declaration);

            return new MethodCompiler(declarationProvider, diagnostics)
                .CompileBody(syntaxTree.Functions[0], declaration, syntaxTree.Namespace, sourceFilename);
        }

        /// <summary>
        /// Parses the given source code, compiles the method declarations and
        /// returns the result of <see cref="MethodCompiler.CompileBody"/> on the first method.
        /// </summary>
        protected CompiledMethod TryCompileFirstMethod([NotNull] string source,
            [NotNull] out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            // Parse the source
            const string sourceFilename = "test.cle";
            var syntaxTree = SyntaxParser.Parse(sourceBytes.AsMemory(), sourceFilename, diagnostics);
            Assert.That(syntaxTree, Is.Not.Null, "Source file was not parsed successfully.");

            // Parse the declarations
            var declarationProvider = new TestingSingleFileDeclarationProvider();
            MethodDeclaration firstDeclaration = null;
            foreach (var functionSyntax in syntaxTree.Functions)
            {
                var declaration = MethodCompiler.CompileDeclaration(functionSyntax, syntaxTree.Namespace, sourceFilename,
                    declarationProvider.Methods.Count, declarationProvider, diagnostics);
                Assert.That(declaration, Is.Not.Null,
                    $"Method declaration for {functionSyntax.Name} was not compiled successfully.");

                if (firstDeclaration is null)
                {
                    firstDeclaration = declaration;
                }
                declarationProvider.Methods.Add(functionSyntax.Name, declaration);
            }

            // Then compile the first method
            return new MethodCompiler(declarationProvider, diagnostics)
                .CompileBody(syntaxTree.Functions[0], firstDeclaration, syntaxTree.Namespace, sourceFilename);
        }
        
        /// <summary>
        /// Verifies that the method has disassembly equal to <paramref name="expected"/>.
        /// Both the actual and expected strings are trimmed and linefeed normalized.
        /// </summary>
        protected void AssertDisassembly([NotNull] CompiledMethod compiledMethod, [NotNull] string expected)
        {
            var builder = new StringBuilder();
            MethodDisassembler.Disassemble(compiledMethod, builder);

            Assert.That(builder.ToString().Trim().Replace("\r\n", "\n"), 
                Is.EqualTo(expected.Trim().Replace("\r\n", "\n")));
        }
    }
}
