using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class AssignmentTests : SyntaxParserTestBase
    {
        [Test]
        public void Assignment_with_integer_constant()
        {
            const string source = @"namespace Test;
private void Function()
{
    answer = 42;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<AssignmentSyntax>());

            var declaration = (AssignmentSyntax)block.Statements[0];
            Assert.That(declaration.Variable, Is.EqualTo("answer"));
            Assert.That(declaration.Value, Is.InstanceOf<IntegerLiteralSyntax>());
        }

        [Test]
        public void Assignment_with_integer_expression()
        {
            const string source = @"namespace Test;
private void Function()
{
    answer = -42;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<AssignmentSyntax>());

            var declaration = (AssignmentSyntax)block.Statements[0];
            Assert.That(declaration.Variable, Is.EqualTo("answer"));
            Assert.That(declaration.Value, Is.InstanceOf<UnaryExpressionSyntax>());
        }

        [Test]
        public void Assignment_with_bool_constant()
        {
            const string source = @"namespace Test;
private void Function()
{
    answer = true;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<AssignmentSyntax>());

            var declaration = (AssignmentSyntax)block.Statements[0];
            Assert.That(declaration.Variable, Is.EqualTo("answer"));
            Assert.That(declaration.Value, Is.InstanceOf<BooleanLiteralSyntax>());
        }

        [Test]
        public void Expression_must_be_valid()
        {
            const string source = @"namespace Test;
private void Function()
{
    value = ;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 4, 12).WithActual(";");
        }

        [Test]
        public void Must_have_semicolon()
        {
            const string source = @"namespace Test;
private void Function()
{
    value = 42
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, 5, 0).WithActual("}");
        }
    }
}
