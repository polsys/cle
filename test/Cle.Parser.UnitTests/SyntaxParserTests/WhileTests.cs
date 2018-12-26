using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class WhileTests : SyntaxParserTestBase
    {
        [Test]
        public void Simple_loop_with_constant_condition()
        {
            const string source = @"namespace Test;
private bool Function()
{
    while (true) {
        return true;
    }
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<WhileStatementSyntax>());

            var loop = (WhileStatementSyntax)block.Statements[0];
            Assert.That(loop.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(loop.BodySyntax.Statements, Has.Exactly(1).Items);
        }

        [Test]
        public void Condition_must_be_valid()
        {
            const string source = @"namespace Test;
private void Error()
{
    while (if) { }
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 4, 11).WithActual("if");
        }

        [Test]
        public void Condition_must_be_within_parens()
        {
            const string source = @"namespace Test;
private void Error()
{
    while true { }
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedCondition, 4, 10).WithActual("true");
        }

        [Test]
        public void Body_must_be_valid()
        {
            const string source = @"namespace Test;
private void Error()
{
    while (true) { 42 }
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedStatement, 4, 19).WithActual("42");
        }

        [Test]
        public void Must_have_body()
        {
            const string source = @"namespace Test;
private void Error()
{
    while (true) return true;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedBlock, 4, 17).WithActual("return");
        }
    }
}
