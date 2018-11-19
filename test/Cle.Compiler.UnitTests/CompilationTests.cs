﻿using System.Collections.Immutable;
using Cle.Common;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class CompilationTests
    {
        [Test]
        public void AddDiagnostics_with_empty_list()
        {
            var compilation = new Compilation();

            compilation.AddDiagnostics(new Diagnostic[] { });

            Assert.That(compilation.HasErrors, Is.False);
            Assert.That(compilation.Diagnostics, Is.Empty);
        }

        [Test]
        public void AddDiagnostics_with_multiple_errors()
        {
            var compilation = new Compilation();

            compilation.AddDiagnostics(new[]
            {
                new Diagnostic(DiagnosticCode.ExpectedSemicolon, default, "", "", null),
                new Diagnostic(DiagnosticCode.ExpectedMethodBody, default, "", "", null)
            });

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(2).Items);
        }

        [Test]
        public void AddDiagnostics_with_warning()
        {
            var compilation = new Compilation();

            // TODO: This could be changed once real warnings exist in the codebase
            compilation.AddDiagnostics(new[]
                { new Diagnostic(DiagnosticCode.SemanticWarningStart, default, "", "", null) });

            Assert.That(compilation.HasErrors, Is.False);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
        }

        [Test]
        public void AddMissingFileError_adds_error()
        {
            var compilation = new Compilation();

            compilation.AddMissingFileError("ModuleName", "FileName.cle");

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("ModuleName"));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("FileName.cle"));
        }

        [Test]
        public void AddMissingModuleError_adds_error()
        {
            var compilation = new Compilation();

            compilation.AddMissingModuleError("ModuleName");

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("ModuleName"));
            Assert.That(compilation.Diagnostics[0].Filename, Is.Null);
        }

        [Test]
        public void AddMethod_succeeds_adding_private_method_with_same_name()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Private, "other.cle", compilation);

            Assert.IsTrue(compilation.AddMethodDeclaration("TestMethod", "Namespace", first));
            Assert.IsTrue(compilation.AddMethodDeclaration("TestMethod", "Namespace", second));
        }

        [Test]
        public void AddMethod_fails_adding_private_method_with_same_name_to_same_file()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);

            Assert.IsTrue(compilation.AddMethodDeclaration("TestMethod", "Namespace", first));
            Assert.IsFalse(compilation.AddMethodDeclaration("TestMethod", "Namespace", second));
        }

        [Test]
        public void AddMethod_fails_adding_private_method_after_public_with_same_name()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Public, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Private, "other.cle", compilation);

            Assert.IsTrue(compilation.AddMethodDeclaration("TestMethod", "Namespace", first));
            Assert.IsFalse(compilation.AddMethodDeclaration("TestMethod", "Namespace", second));
        }

        [Test]
        public void AddMethod_fails_adding_public_method_after_private_with_same_name()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Public, "other.cle", compilation);

            Assert.IsTrue(compilation.AddMethodDeclaration("TestMethod", "Namespace", first));
            Assert.IsFalse(compilation.AddMethodDeclaration("TestMethod", "Namespace", second));
        }

        [Test]
        public void GetMethodDeclarations_gets_public_method()
        {
            var compilation = new Compilation();
            var declaration = CreateDeclaration("TestMethod", Visibility.Public, "test.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", declaration);

            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "another.cle");

            Assert.That(result, Has.Exactly(1).Items);
            Assert.That(result[0], Is.SameAs(declaration));
        }

        [Test]
        public void GetMethodDeclarations_gets_internal_method_in_same_module()
        {
            // TODO: This test is not very useful yet because modules do not exist
            var compilation = new Compilation();
            var declaration = CreateDeclaration("TestMethod", Visibility.Internal, "test.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", declaration);

            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "another.cle");

            Assert.That(result, Has.Exactly(1).Items);
            Assert.That(result[0], Is.SameAs(declaration));
        }

        // TODO: Test that internal methods are not visible outside the defining module, once modules exist

        [Test]
        public void GetMethodDeclarations_gets_correct_private_method()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Private, "another.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", first);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", second);

            var firstResult = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "test.cle");
            CollectionAssert.AreEquivalent(new[] { first }, firstResult);

            var secondResult = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "another.cle");
            CollectionAssert.AreEquivalent(new[] { second }, secondResult);
        }

        [Test]
        public void GetMethodDeclarations_gets_multiple_matching_methods()
        {
            var compilation = new Compilation();
            var first = CreateDeclaration("TestMethod", Visibility.Public, "test.cle", compilation);
            var second = CreateDeclaration("TestMethod", Visibility.Public, "another.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Test", first);
            compilation.AddMethodDeclaration("TestMethod", "Another", second);

            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "Test", "Another" }, "more.cle");
            
            CollectionAssert.AreEquivalent(new[] { first, second }, result);
        }

        [Test]
        public void GetMethodDeclarations_returns_nothing_if_method_does_not_exist()
        {
            var compilation = new Compilation();
            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "another.cle");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetMethodDeclarations_returns_nothing_if_method_not_in_namespace()
        {
            var compilation = new Compilation();
            var declaration = CreateDeclaration("TestMethod", Visibility.Public, "test.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", declaration);

            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "OtherNamespace" }, "another.cle");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetMethodDeclarations_returns_nothing_if_private_method_is_not_visible()
        {
            var compilation = new Compilation();
            var declaration = CreateDeclaration("TestMethod", Visibility.Private, "test.cle", compilation);
            compilation.AddMethodDeclaration("TestMethod", "Namespace", declaration);

            var result = compilation.GetMethodDeclarations("TestMethod", new[] { "Namespace" }, "another.cle");

            Assert.That(result, Is.Empty);
        }

        private static MethodDeclaration CreateDeclaration(
            [NotNull] string methodName,
            Visibility visibility,
            [NotNull] string filename,
            [NotNull] Compilation compilation)
        {
            var diagnostics = new SingleFileDiagnosticSink(".", filename);
            var methodSyntax = new FunctionSyntax(methodName, "bool", visibility,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var declaration = MethodCompiler.CompileDeclaration(methodSyntax, filename, compilation, diagnostics);

            // Sanity check
            Assert.That(declaration, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            return declaration;
        }
    }
}
