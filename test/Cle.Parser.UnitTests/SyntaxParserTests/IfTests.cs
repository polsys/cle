using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class IfTests : SyntaxParserTestBase
    {
        [Test]
        public void Empty_if_and_else_is_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) {} else {}
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<IfStatementSyntax>());
            var statement = (IfStatementSyntax)block.Statements[0];

            Assert.That(statement.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)statement.ConditionSyntax).Value, Is.True);
            Assert.That(statement.ThenBlockSyntax, Is.Not.Null);
            Assert.That(statement.ThenBlockSyntax.Statements, Is.Empty);
            Assert.That(statement.ElseSyntax, Is.Not.Null);
            Assert.That(statement.ElseSyntax, Is.InstanceOf<BlockSyntax>());
            Assert.That(((BlockSyntax)statement.ElseSyntax).Statements, Is.Empty);
        }

        [Test]
        public void Nonempty_if_and_else_is_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) { return true; } else { return false; return false; }
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<IfStatementSyntax>());
            var statement = (IfStatementSyntax)block.Statements[0];

            Assert.That(statement.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)statement.ConditionSyntax).Value, Is.True);
            Assert.That(statement.ThenBlockSyntax, Is.Not.Null);
            Assert.That(statement.ThenBlockSyntax.Statements, Has.Exactly(1).Items);
            Assert.That(statement.ElseSyntax, Is.Not.Null);
            Assert.That(statement.ElseSyntax, Is.InstanceOf<BlockSyntax>());
            Assert.That(((BlockSyntax)statement.ElseSyntax).Statements, Has.Exactly(2).Items);
        }

        [Test]
        public void Empty_if_without_else_is_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) {}
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<IfStatementSyntax>());
            var statement = (IfStatementSyntax)block.Statements[0];

            Assert.That(statement.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)statement.ConditionSyntax).Value, Is.True);
            Assert.That(statement.ThenBlockSyntax, Is.Not.Null);
            Assert.That(statement.ThenBlockSyntax.Statements, Is.Empty);
            Assert.That(statement.ElseSyntax, Is.Null);
        }

        [Test]
        public void Empty_else_if_is_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) {} else if (false) {} else {}
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<IfStatementSyntax>());
            var statement = (IfStatementSyntax)block.Statements[0];

            Assert.That(statement.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)statement.ConditionSyntax).Value, Is.True);
            Assert.That(statement.ThenBlockSyntax, Is.Not.Null);
            Assert.That(statement.ThenBlockSyntax.Statements, Is.Empty);
            Assert.That(statement.ElseSyntax, Is.Not.Null);
            Assert.That(statement.ElseSyntax, Is.InstanceOf<IfStatementSyntax>());

            var elseStatement = (IfStatementSyntax)statement.ElseSyntax;
            Assert.That(elseStatement.ConditionSyntax, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)elseStatement.ConditionSyntax).Value, Is.False);
            Assert.That(elseStatement.ThenBlockSyntax, Is.Not.Null);
            Assert.That(elseStatement.ThenBlockSyntax.Statements, Is.Empty);
            Assert.That(elseStatement.ElseSyntax, Is.Not.Null);
            Assert.That(elseStatement.ElseSyntax, Is.InstanceOf<BlockSyntax>());
            Assert.That(((BlockSyntax)elseStatement.ElseSyntax).Statements, Is.Empty);
        }

        [Test]
        public void Missing_condition_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if {} else {}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedCondition, 4, 7).WithActual("{");
        }

        [Test]
        public void Empty_condition_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if () {} else {}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 4, 8);
        }

        [Test]
        public void Missing_parens_around_condition_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if true {} else {}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedCondition, 4, 7).WithActual("true");
        }

        [Test]
        public void Missing_close_paren_in_condition_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true {} else {}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingParen, 4, 13).WithActual("{");
        }

        [Test]
        public void Missing_brace_for_block_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) return false;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedBlock, 4, 14).WithActual("return");
        }

        [Test]
        public void Missing_brace_for_else_block_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    if (true) { return false; } else return true;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedBlockOrElseIf, 4, 37).WithActual("return");
        }

        [Test]
        public void Else_without_if_is_error()
        {
            const string source = @"namespace Test;
private void Function()
{
    else { return false; }
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            Assert.IsNull(syntaxTree);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ElseWithoutIf, 4, 4);
        }
    }
}
