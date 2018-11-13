using System.Collections.Immutable;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class MethodCompilerTests
    {
        [Test]
        public void CompileDeclaration_parameterless_bool_method_succeeds()
        {
            var syntax = new FunctionSyntax("MethodName", "bool", Visibility.Public,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodCompiler.CompileDeclaration(syntax, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ReturnType, Is.EqualTo(SimpleType.Bool));
        }

        [Test]
        public void CompileDeclaration_parameterless_int32_method_succeeds()
        {
            var syntax = new FunctionSyntax("MethodName", "int32", Visibility.Public,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodCompiler.CompileDeclaration(syntax, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ReturnType, Is.EqualTo(SimpleType.Int32));
        }

        [Test]
        public void CompileDeclaration_parameterless_method_with_unknown_type_fails()
        {
            var syntax = new FunctionSyntax("MethodName", "UltimateBool", Visibility.Public,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), new TextPosition(3, 1, 3));
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodCompiler.CompileDeclaration(syntax, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeNotFound, 1, 3).WithActual("UltimateBool");
        }
    }
}
