using System.Text;
using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class AttributeTests : SyntaxParserTestBase
    {
        [Test]
        public void Two_attributes_without_parameters()
        {
            const string source = @"namespace Test;
[Attribute1]
[Attribute2]
private void Function() {}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);
            var function = syntaxTree.Functions[0];

            Assert.That(function.Attributes, Has.Exactly(2).Items);
            Assert.That(function.Attributes[0].Name, Is.EqualTo("Attribute1"));
            Assert.That(function.Attributes[0].Position.Line, Is.EqualTo(2));
            Assert.That(function.Attributes[0].Parameters, Is.Empty);
            Assert.That(function.Attributes[1].Name, Is.EqualTo("Attribute2"));
            Assert.That(function.Attributes[1].Position.Line, Is.EqualTo(3));
            Assert.That(function.Attributes[1].Parameters, Is.Empty);
            Assert.That(function.Position.Line, Is.EqualTo(4));
        }

        [Test]
        public void Attribute_with_empty_parameter_list()
        {
            const string source = @"namespace Test;
[Attribute()]
private void Function() {}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);
            var function = syntaxTree.Functions[0];

            Assert.That(function.Attributes, Has.Exactly(1).Items);
            Assert.That(function.Attributes[0].Parameters, Is.Empty);
        }

        [Test]
        public void Attribute_with_one_parameter()
        {
            const string source = @"namespace Test;
[Attribute(true)]
private void Function() {}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);
            var function = syntaxTree.Functions[0];

            Assert.That(function.Attributes, Has.Exactly(1).Items);
            Assert.That(function.Attributes[0].Name, Is.EqualTo("Attribute"));
            Assert.That(function.Attributes[0].Parameters, Has.Exactly(1).Items);

            var param = function.Attributes[0].Parameters[0];
            Assert.That(param, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)param).Value, Is.True);
            Assert.That(param.Position.Line, Is.EqualTo(2));
            Assert.That(param.Position.ByteInLine, Is.EqualTo(11));
        }

        [Test]
        public void Attribute_with_three_parameters()
        {
            const string source = @"namespace Test;
[Attribute(42, false,
 ""a string"")]
private void Function() {}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);
            var function = syntaxTree.Functions[0];

            Assert.That(function.Attributes, Has.Exactly(1).Items);
            Assert.That(function.Attributes[0].Name, Is.EqualTo("Attribute"));
            Assert.That(function.Attributes[0].Parameters, Has.Exactly(3).Items);

            var param1 = function.Attributes[0].Parameters[0];
            Assert.That(param1, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)param1).Value, Is.EqualTo(42));
            Assert.That(param1.Position.Line, Is.EqualTo(2));
            Assert.That(param1.Position.ByteInLine, Is.EqualTo(11));

            var param2 = function.Attributes[0].Parameters[1];
            Assert.That(param2, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)param2).Value, Is.False);
            Assert.That(param2.Position.Line, Is.EqualTo(2));
            Assert.That(param2.Position.ByteInLine, Is.EqualTo(15));

            var param3 = function.Attributes[0].Parameters[2];
            Assert.That(param3, Is.InstanceOf<StringLiteralSyntax>());
            Assert.That(Encoding.UTF8.GetString(((StringLiteralSyntax)param3).Value), Is.EqualTo("a string"));
            Assert.That(param3.Position.Line, Is.EqualTo(3));
            Assert.That(param3.Position.ByteInLine, Is.EqualTo(1));
        }

        [Test]
        public void Attribute_name_must_exist()
        {
            const string source = @"namespace Test;
[]
private void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedAttributeName, 2, 1).WithActual("]");
        }

        [Test]
        public void Keyword_is_not_valid_attribute_name()
        {
            const string source = @"namespace Test;
[if]
private void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedAttributeName, 2, 1).WithActual("if");
        }

        [Test]
        public void Must_have_closing_bracket()
        {
            const string source = @"namespace Test;
[Attribute
private void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingBracket, 3, 0).WithActual("private");
        }

        [Test]
        public void Cannot_apply_attribute_to_namespace()
        {
            const string source = "[AttributeType] namespace Test;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.AttributesOnlyApplicableToFunctions, 1, 16);
        }

        [Test]
        public void Attribute_parameter_must_be_literal()
        {
            const string source = @"namespace Test;
[Attribute(1 + 1)]
public void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.AttributeParameterMustBeLiteral, 2, 13);
        }

        [Test]
        public void Keyword_is_not_valid_attribute_parameter()
        {
            const string source = @"namespace Test;
[Attribute(if)]
public void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 2, 11);
        }

        [Test]
        public void Attribute_parameter_list_with_extra_comma()
        {
            const string source = @"namespace Test;
[Attribute(1,)]
public void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 2, 13);
        }

        [Test]
        public void Attribute_parameter_list_without_comma()
        {
            const string source = @"namespace Test;
[Attribute(1 2)]
public void Function() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(syntaxTree, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingParen, 2, 13);
        }
    }
}
