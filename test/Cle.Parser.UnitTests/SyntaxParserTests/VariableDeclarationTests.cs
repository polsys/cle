using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class VariableDeclarationTests : SyntaxParserTestBase
    {
        [Test]
        public void Int32_declaration_with_constant_value_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 answer = 42;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("answer"));
            Assert.That(declaration.Type, Is.EqualTo("int32"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(declaration.Position.Line, Is.EqualTo(4));
            Assert.That(declaration.Position.ByteInLine, Is.EqualTo(4));

            Assert.That(declaration.Type.Position.ByteInLine, Is.EqualTo(8));
            Assert.That(declaration.InitialValueExpression.Position.ByteInLine, Is.EqualTo(23));
        }

        [Test]
        public void Int32_declaration_with_expression_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 answer = 2 * 21;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("answer"));
            Assert.That(declaration.Type, Is.EqualTo("int32"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BinaryExpressionSyntax>());
        }

        [Test]
        public void Bool_declaration_with_constant_value_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    var bool _is_42_THE_answer = true;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("_is_42_THE_answer"));
            Assert.That(declaration.Type, Is.EqualTo("bool"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BooleanLiteralSyntax>());
        }

        [Test]
        public void Custom_typed_declaration_parsed_correctly()
        {
            // Of course, 'true' might not be convertible to the type, but that's a semantic issue
            const string source = @"namespace Test;
private void Function()
{
    var UserDefined::TypeName variable = true;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("variable"));
            Assert.That(declaration.Type, Is.EqualTo("UserDefined::TypeName"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BooleanLiteralSyntax>());
        }

        [Test]
        public void Declaration_must_have_initial_value()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 value;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedInitialValue, 4, 19).WithActual(";");
        }

        [Test]
        public void Initial_value_expression_must_be_valid()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 value = ;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 4, 22).WithActual(";");
        }

        [Test]
        public void Single_underscore_is_not_valid_type_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var _ value = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidTypeName, 4, 8).WithActual("_");
        }

        [Test]
        public void Single_underscore_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 _ = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 14).WithActual("_");
        }

        [Test]
        public void Double_underscore_is_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 __ = 0;
}";
            var block = ParseBlockForSingleFunction(source);
            Assert.That(block, Is.Not.Null);
        }

        [Test]
        public void Keyword_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 if = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedIdentifier, 4, 14).WithActual("if");
        }

        [Test]
        public void Keyword_is_not_valid_variable_type()
        {
            const string source = @"namespace Test;
private void Function()
{
    var if name = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedType, 4, 8).WithActual("if");
        }

        [Test]
        public void Reserved_type_name_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 int32 = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 14).WithActual("int32");
        }

        [Test]
        public void Fully_qualified_name_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    var int32 Something::or::other = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 14).WithActual("Something::or::other");
        }

        [Test]
        public void Sane_error_when_missing_var_keyword()
        {
            const string source = @"namespace Test;
private void Function()
{
    type_name missing_var = 42;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedStatement, 4, 4);
        }
    }
}
