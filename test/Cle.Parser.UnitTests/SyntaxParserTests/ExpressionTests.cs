using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class ExpressionTests : SyntaxParserTestBase
    {
        [TestCase("0", 0ul)]
        [TestCase("1234567890", 1234567890ul)]
        [TestCase("007", 7ul)]
        [TestCase("18446744073709551615", ulong.MaxValue)]
        public void Valid_integer_literals(string source, ulong expected)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);
            Assert.That(expression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)expression).Value, Is.EqualTo(expected));
        }

        [TestCase("0xDEAD")]
        [TestCase("1`234`567`890")]
        [TestCase("18446744073709551616")] // Too big to fit in uint64
        public void Invalid_integer_literals(string source)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidNumericLiteral, 1, 0);
        }
    }
}
