using Cle.Common;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class NamespaceTests : SyntaxParserTestBase
    {
        [Test]
        public void Namespace_is_correctly_read()
        {
            const string source = "namespace NamespaceName;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree.Namespace, Is.EqualTo("NamespaceName"));
        }

        [Test]
        public void Namespace_multipart_is_correctly_read()
        {
            const string source = "namespace Namespace::Name::_Part3;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(syntaxTree, Is.Not.Null);
            Assert.That(syntaxTree.Namespace, Is.EqualTo("Namespace::Name::_Part3"));
        }
        
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
                .WithActual(name);
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_keyword_as_name_is_rejected()
        {
            const string source = "namespace namespace;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceName, new TextPosition(10, 1, 10))
                .WithActual("namespace");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_name_starting_with_digit_is_rejected()
        {
            // The lexer interprets '3D' as a number so the name validation path is not reached
            const string source = "namespace 3D;";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceName, new TextPosition(10, 1, 10))
                .WithActual("3D");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_missing_name_is_rejected()
        {
            const string source = "namespace;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceName, new TextPosition(9, 1, 9))
                .WithActual(";");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_be_followed_by_semicolon_not_eof()
        {
            const string source = "namespace Namespace";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, new TextPosition(19, 1, 19))
                .WithActual("");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_be_followed_by_semicolon_not_something_else()
        {
            const string source = "namespace Namespace something";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, new TextPosition(20, 1, 20))
                .WithActual("something");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_may_not_exist_twice()
        {
            const string source = "namespace Something;\nnamespace Else;";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedOnlyOneNamespace, new TextPosition(21, 2, 0));
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Namespace_declaration_must_precede_function_definition()
        {
            const string source = "public void Something() {}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedNamespaceDeclarationBeforeDefinitions,
                new TextPosition(0, 1, 0));
            Assert.That(syntaxTree, Is.Null);
        }
    }
}
