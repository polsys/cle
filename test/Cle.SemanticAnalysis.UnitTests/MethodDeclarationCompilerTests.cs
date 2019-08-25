using System.Collections.Immutable;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class MethodDeclarationCompilerTests
    {
        [Test]
        public void Compile_parameterless_bool_method_succeeds()
        {
            var position = new TextPosition(13, 3, 4);
            var syntax = new FunctionSyntax("MethodName", "bool",
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "Namespace", "bool.cle", 7, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsEntryPoint, Is.False);
            Assert.That(result.ReturnType, Is.EqualTo(SimpleType.Bool));
            Assert.That(result.Visibility, Is.EqualTo(Visibility.Public));
            Assert.That(result.FullName, Is.EqualTo("Namespace::MethodName"));
            Assert.That(result.DefiningFilename, Is.EqualTo("bool.cle"));
            Assert.That(result.DefinitionPosition, Is.EqualTo(position));
            Assert.That(result.BodyIndex, Is.EqualTo(7));
        }

        [Test]
        public void Compile_parameterless_int32_method_succeeds()
        {
            var position = new TextPosition(280, 14, 8);
            var syntax = new FunctionSyntax("MethodName", "int32",
                Visibility.Private, ImmutableList<ParameterDeclarationSyntax>.Empty, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "long::ns", "int32.cle", 8, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsEntryPoint, Is.False);
            Assert.That(result.ReturnType, Is.EqualTo(SimpleType.Int32));
            Assert.That(result.Visibility, Is.EqualTo(Visibility.Private));
            Assert.That(result.FullName, Is.EqualTo("long::ns::MethodName"));
            Assert.That(result.DefiningFilename, Is.EqualTo("int32.cle"));
            Assert.That(result.DefinitionPosition, Is.EqualTo(position));
            Assert.That(result.BodyIndex, Is.EqualTo(8));
        }

        [Test]
        public void Compile_parameterless_method_with_unknown_type_fails()
        {
            var syntax = new FunctionSyntax("MethodName", "UltimateBool", 
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), new TextPosition(3, 1, 3));
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "unknown.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeNotFound, 1, 3).WithActual("UltimateBool");
        }
        
        [Test]
        public void Compile_method_with_parameters_succeeds()
        {
            var parameters = ImmutableList<ParameterDeclarationSyntax>.Empty
                .Add(new ParameterDeclarationSyntax("int32", "intParam", default))
                .Add(new ParameterDeclarationSyntax("bool", "boolParam", default));

            var syntax = new FunctionSyntax("MethodName", "int32",
                Visibility.Private, parameters, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "int32.cle", 0, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ParameterTypes, Has.Exactly(2).Items);
            Assert.That(result.ParameterTypes[0], Is.EqualTo(SimpleType.Int32));
            Assert.That(result.ParameterTypes[1], Is.EqualTo(SimpleType.Bool));
        }

        [Test]
        public void Compile_parameter_type_must_exist()
        {
            var position = new TextPosition(140, 13, 4);
            var parameters = ImmutableList<ParameterDeclarationSyntax>.Empty
                .Add(new ParameterDeclarationSyntax("NonExistentType", "param", position));

            var syntax = new FunctionSyntax("MethodName", "int32",
                Visibility.Private, parameters, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "unknown.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeNotFound, position.Line, position.ByteInLine)
                .WithActual("NonExistentType");
        }

        [Test]
        public void Compile_parameter_type_must_not_be_void()
        {
            var position = new TextPosition(140, 13, 4);
            var parameters = ImmutableList<ParameterDeclarationSyntax>.Empty
                .Add(new ParameterDeclarationSyntax("void", "param", position));

            var syntax = new FunctionSyntax("MethodName", "int32",
                Visibility.Private, parameters, ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "unknown.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VoidIsNotValidType, position.Line, position.ByteInLine)
                .WithActual("param");
        }

        [Test]
        public void Compile_does_not_accept_unknown_attribute()
        {
            var position = new TextPosition(140, 13, 4);
            var parameters = ImmutableList<LiteralSyntax>.Empty;
            var attribute = new AttributeSyntax("TotallyNonexistentAttribute", parameters, position);
            var syntax = new FunctionSyntax("MethodName", "bool",
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(attribute),
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "unknown.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnknownAttribute, position.Line, position.ByteInLine)
                .WithActual("TotallyNonexistentAttribute");
        }

        [Test]
        public void Compile_entry_point_is_flagged()
        {
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Public, "int32", null);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsEntryPoint, Is.True);
        }

        [Test]
        public void Compile_entry_point_may_be_private()
        {
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Private, "int32", null);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsEntryPoint, Is.True);
        }

        [Test]
        public void Compile_entry_point_must_return_int32()
        {
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Public, "bool", null);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.EntryPointMustBeDeclaredCorrectly, 1, 3);
        }

        [Test]
        public void Compile_entry_point_must_not_have_parameters()
        {
            var parameter = new ParameterDeclarationSyntax("int32", "param", default);
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Public, "int32", parameter);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.EntryPointMustBeDeclaredCorrectly, 1, 3);
        }

        private static (MethodDeclaration?, TestingDiagnosticSink) CompileEntryPointDeclaration(
            Visibility visibility, string returnType, ParameterDeclarationSyntax? parameter)
        {
            var paramList = ImmutableList<ParameterDeclarationSyntax>.Empty;
            if (parameter != null)
            {
                paramList = paramList.Add(parameter);
            }

            var entryPointAttribute = new AttributeSyntax("EntryPoint", ImmutableList<LiteralSyntax>.Empty, default);
            var syntax = new FunctionSyntax("Main", returnType,
                visibility, paramList, ImmutableList<AttributeSyntax>.Empty.Add(entryPointAttribute),
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), new TextPosition(3, 1, 3));

            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "entrypoint.cle", 0, declarationProvider, diagnostics);
            return (result, diagnostics);
        }
    }
}
