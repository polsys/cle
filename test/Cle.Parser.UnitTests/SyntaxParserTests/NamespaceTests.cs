using Cle.Common;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class NamespaceTests : SyntaxParserTestBase
    {
        [Test]
        public void Namespace_is_correctly_read()
        {
            var source = "namespace NamespaceName;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree.Namespace, Is.EqualTo("NamespaceName"));
        }

        [Test]
        public void Namespace_multipart_is_correctly_read()
        {
            var source = "namespace Namespace::Name::_Part3;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree.Namespace, Is.EqualTo("Namespace::Name::_Part3"));
        }

        [TestCase("3D")]
        [TestCase("Som\x00E9thing")]
        [TestCase("::Namespace")]
        [TestCase("Namespace::")]
        [TestCase("Missing::::Part")]
        [TestCase("Too:::Many")]
        public void Namespace_invalid_name_is_rejected(string name)
        {
            var source = $"namespace {name};";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidNamespaceName, new TextPosition(10, 1, 10))
                .WithNonNullActual();
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_keyword_as_name_is_rejected()
        {
            var source = "namespace namespace;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceName, new TextPosition(10, 1, 10))
                .WithActual("namespace");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_missing_name_is_rejected()
        {
            var source = "namespace;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceName, new TextPosition(9, 1, 9))
                .WithActual(";");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_be_followed_by_semicolon_not_eof()
        {
            var source = "namespace Namespace";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, new TextPosition(19, 1, 19))
                .WithActual("");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_be_followed_by_semicolon_not_something_else()
        {
            var source = "namespace Namespace something";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, new TextPosition(20, 1, 20))
                .WithActual("something");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_may_not_exist_twice()
        {
            var source = "namespace Something;\nnamespace Else;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedOnlyOneNamespace, new TextPosition(21, 2, 0));
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_precede_function_definition()
        {
            var source = "public void Something() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceDeclarationBeforeDefinitions,
                new TextPosition(0, 1, 0));
            Assert.That(syntaxTree, Is.Null);
        }
    }
}
