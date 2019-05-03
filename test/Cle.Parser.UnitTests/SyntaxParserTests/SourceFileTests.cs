using Cle.Common;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class SourceFileTests : SyntaxParserTestBase
    {
        [Test]
        public void Statement_not_allowed_in_global_level()
        {
            const string source = @"if (true) {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSourceFileItem, new TextPosition(0, 1, 0));
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void File_name_is_recorded()
        {
            const string source = @"namespace Test;";
            var syntaxTree = ParseSource(source, out _);
            
            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree!.Filename, Is.EqualTo("test.cle")); // Default of ParseSource(...)
        }
    }
}
