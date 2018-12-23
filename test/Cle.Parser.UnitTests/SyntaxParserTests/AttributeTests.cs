using Cle.Common;
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
            Assert.That(function.Attributes[1].Name, Is.EqualTo("Attribute2"));
            Assert.That(function.Attributes[1].Position.Line, Is.EqualTo(3));
            Assert.That(function.Position.Line, Is.EqualTo(4));
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
    }
}
