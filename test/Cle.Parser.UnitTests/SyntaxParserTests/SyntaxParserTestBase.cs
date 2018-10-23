using System;
using System.Text;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class SyntaxParserTestBase
    {
        [CanBeNull]
        protected SourceFileSyntax ParseSource([NotNull] string source, out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            return SyntaxParser.Parse(sourceBytes.AsMemory(), diagnostics);
        }

        [NotNull]
        protected SourceFileSyntax ParseSourceWithoutDiagnostics([NotNull] string source)
        {
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty, "Expected no diagnostics");
            Assert.That(syntaxTree, Is.Not.Null, "Expected non-null syntax tree");

            return syntaxTree;
        }

        [NotNull]
        protected SyntaxParser GetParserInstance([NotNull] string source, out TestingDiagnosticSink diagnostics)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            diagnostics = new TestingDiagnosticSink();

            return new SyntaxParser(sourceBytes.AsMemory(), diagnostics);
        }
    }
}
