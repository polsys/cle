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
    int32 answer = 42;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("answer"));
            Assert.That(declaration.TypeName, Is.EqualTo("int32"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<IntegerLiteralSyntax>());
        }

        [Test]
        public void Int32_declaration_with_expression_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 answer = 2 * 21;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("answer"));
            Assert.That(declaration.TypeName, Is.EqualTo("int32"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BinaryExpressionSyntax>());
        }

        [Test]
        public void Bool_declaration_with_constant_value_parsed_correctly()
        {
            const string source = @"namespace Test;
private void Function()
{
    bool _is_42_THE_answer = true;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("_is_42_THE_answer"));
            Assert.That(declaration.TypeName, Is.EqualTo("bool"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BooleanLiteralSyntax>());
        }

        [Test]
        public void Custom_typed_declaration_parsed_correctly()
        {
            // Of course, 'true' might not be convertible to the type, but that's a semantic issue
            const string source = @"namespace Test;
private void Function()
{
    UserDefined::TypeName variable = true;
}";
            var block = ParseBlockForSingleFunction(source);

            Assert.That(block.Statements, Has.Exactly(1).Items);
            Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclarationSyntax>());

            var declaration = (VariableDeclarationSyntax)block.Statements[0];
            Assert.That(declaration.Name, Is.EqualTo("variable"));
            Assert.That(declaration.TypeName, Is.EqualTo("UserDefined::TypeName"));
            Assert.That(declaration.InitialValueExpression, Is.InstanceOf<BooleanLiteralSyntax>());
        }

        [Test]
        public void Declaration_must_have_initial_value()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 value;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedInitialValue, 4, 15).WithActual(";");
        }

        [Test]
        public void Initial_value_expression_must_be_valid()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 value = ;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 4, 18).WithActual(";");
        }

        [Test]
        public void Single_underscore_is_not_valid_type_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    _ value = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidTypeName, 4, 4).WithActual("_");
        }

        [Test]
        public void Single_underscore_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 _ = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 10).WithActual("_");
        }

        [Test]
        public void Double_underscore_is_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 __ = 0;
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
    int32 if = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            // This throws a different error as 'if' is not an identifier.
            // The error should be at 'int32' because that's where the statement should start.
            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedStatement, 4, 4).WithActual("if");
        }

        [Test]
        public void Reserved_type_name_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 int32 = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 10).WithActual("int32");
        }

        [Test]
        public void Fully_qualified_name_is_not_valid_variable_name()
        {
            const string source = @"namespace Test;
private void Function()
{
    int32 Something::or::other = 0;
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 4, 10).WithActual("Something::or::other");
        }
    }
}
