using System;
using System.Text;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class SyntaxParserTestBase
    {
        /// <summary>
        /// Parses the given source text and returns the syntax tree if successful.
        /// </summary>
        [CanBeNull]
        protected SourceFileSyntax ParseSource([NotNull] string source, out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            return SyntaxParser.Parse(sourceBytes.AsMemory(), diagnostics);
        }

        /// <summary>
        /// Parses the given source text and asserts if there were parsing errors.
        /// </summary>
        [NotNull]
        protected SourceFileSyntax ParseSourceWithoutDiagnostics([NotNull] string source)
        {
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty, "Expected no diagnostics");
            Assert.That(syntaxTree, Is.Not.Null, "Expected non-null syntax tree");

            return syntaxTree;
        }

        /// <summary>
        /// Parses the given source text for a full file with exactly one function,
        /// asserts that there were no parsing errors and returns the code block for that function.
        /// </summary>
        [NotNull]
        protected BlockSyntax ParseBlockForSingleFunction(string source)
        {
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items, "File must have only a single function");
            var function = syntaxTree.Functions[0];

            return function.Block;
        }

        /// <summary>
        /// Creates and returns a <see cref="SyntaxParser"/> instance for the given source text.
        /// This allows testing instance methods directly.
        /// </summary>
        [NotNull]
        protected SyntaxParser GetParserInstance([NotNull] string source, out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            return new SyntaxParser(sourceBytes.AsMemory(), diagnostics);
        }
    }
}
