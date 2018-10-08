using System;
using System.Text;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;

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
    }
}
