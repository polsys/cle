using System.Collections.Immutable;
using System.Text;
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
            var syntax = MakeParameterlessMethod(Visibility.Public, "bool", position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "Namespace", "bool.cle", 7, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.InstanceOf<NativeMethodDeclaration>());
            Assert.That((result as NativeMethodDeclaration)!.IsEntryPoint, Is.False);
            Assert.That(result!.ReturnType, Is.EqualTo(SimpleType.Bool));
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
            var syntax = MakeParameterlessMethod(Visibility.Private, "int32", position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "long::ns", "int32.cle", 8, declarationProvider, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.InstanceOf<NativeMethodDeclaration>());
            Assert.That((result as NativeMethodDeclaration)!.IsEntryPoint, Is.False);
            Assert.That(result!.ReturnType, Is.EqualTo(SimpleType.Int32));
            Assert.That(result.Visibility, Is.EqualTo(Visibility.Private));
            Assert.That(result.FullName, Is.EqualTo("long::ns::MethodName"));
            Assert.That(result.DefiningFilename, Is.EqualTo("int32.cle"));
            Assert.That(result.DefinitionPosition, Is.EqualTo(position));
            Assert.That(result.BodyIndex, Is.EqualTo(8));
        }

        [Test]
        public void Compile_parameterless_method_with_unknown_type_fails()
        {
            var typePosition = new TextPosition(3, 1, 3);
            var syntax = new FunctionSyntax("MethodName", new TypeNameSyntax("UltimateBool", typePosition),
                   Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                   ImmutableList<AttributeSyntax>.Empty,
                   new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
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
                .Add(MakeParameter("int32", "intParam", default))
                .Add(MakeParameter("bool", "boolParam", default));

            var syntax = new FunctionSyntax("MethodName", MakeType("int32"),
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
                .Add(MakeParameter("NonExistentType", "param", position));

            var syntax = new FunctionSyntax("MethodName", MakeType("int32"),
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
                .Add(MakeParameter("void", "param", position));

            var syntax = new FunctionSyntax("MethodName", MakeType("int32"),
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
            var attributeParams = ImmutableList<LiteralSyntax>.Empty;
            var attribute = new AttributeSyntax("TotallyNonexistentAttribute", attributeParams, position);
            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
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
        public void Compile_raises_error_for_each_unknown_attribute()
        {
            var attributeParams = ImmutableList<LiteralSyntax>.Empty;
            var position1 = new TextPosition(140, 13, 4);
            var position2 = new TextPosition(280, 17, 6);
            var attribute1 = new AttributeSyntax("TotallyNonexistentAttribute", attributeParams, position1);
            var attribute2 = new AttributeSyntax("AnotherNonexistentAttribute", attributeParams, position2);

            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(attribute1).Add(attribute2),
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "unknown.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnknownAttribute, position1.Line, position1.ByteInLine)
                .WithActual("TotallyNonexistentAttribute");
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnknownAttribute, position2.Line, position2.ByteInLine)
                .WithActual("AnotherNonexistentAttribute");
        }

        [Test]
        public void Compile_entry_point_is_flagged()
        {
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Public, "int32", null);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.InstanceOf<NativeMethodDeclaration>());
            Assert.That((result as NativeMethodDeclaration)!.IsEntryPoint, Is.True);
        }

        [Test]
        public void Compile_entry_point_may_be_private()
        {
            var (result, diagnostics) = CompileEntryPointDeclaration(Visibility.Private, "int32", null);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(result, Is.InstanceOf<NativeMethodDeclaration>());
            Assert.That((result as NativeMethodDeclaration)!.IsEntryPoint, Is.True);
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
            var parameter = MakeParameter("int32", "param", default);
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

            var entryPointAttribute = MakeEntryPointAttribute();
            var syntax = new FunctionSyntax("Main", MakeType(returnType),
                visibility, paramList, ImmutableList<AttributeSyntax>.Empty.Add(entryPointAttribute),
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default), new TextPosition(3, 1, 3));

            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "entrypoint.cle", 0, declarationProvider, diagnostics);
            return (result, diagnostics);
        }

        [Test]
        public void Compile_imported_method()
        {
            var position = new TextPosition(17, 2, 3);
            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(MakeImportAttribute("Method", "Library")),
                null, position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.InstanceOf<ImportedMethodDeclaration>());
            var import = (result as ImportedMethodDeclaration)!;
            Assert.That(import.ImportName, Is.EqualTo(Encoding.UTF8.GetBytes("Method")));
            Assert.That(import.ImportLibrary, Is.EqualTo(Encoding.UTF8.GetBytes("Library")));
            Assert.That(import.DefinitionPosition, Is.EqualTo(position));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
        }

        [TestCase("")]
        [TestCase("clé")]
        public void Compile_import_attribute_with_invalid_name(string name)
        {
            var paramList = ImmutableList<LiteralSyntax>.Empty
                .Add(new StringLiteralSyntax(Encoding.UTF8.GetBytes(name), new TextPosition(0, 1, 2)))
                .Add(new StringLiteralSyntax(Encoding.UTF8.GetBytes("lib"), new TextPosition(0, 3, 4)));

            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(new AttributeSyntax("Import", paramList, default)),
                null, default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ImportParameterNotValid, paramList[0].Position);
        }

        [TestCase("")]
        [TestCase("clé")]
        public void Compile_import_attribute_with_invalid_library(string name)
        {
            var paramList = ImmutableList<LiteralSyntax>.Empty
                .Add(new StringLiteralSyntax(Encoding.UTF8.GetBytes("fun"), new TextPosition(0, 1, 2)))
                .Add(new StringLiteralSyntax(Encoding.UTF8.GetBytes(name), new TextPosition(0, 3, 4)));

            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(new AttributeSyntax("Import", paramList, default)),
                null, default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ImportParameterNotValid, paramList[1].Position);
        }

        [Test]
        public void Compile_import_attribute_with_no_parameters()
        {
            var attributePosition = new TextPosition(1024, 58, 3);
            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(
                    new AttributeSyntax("Import", ImmutableList<LiteralSyntax>.Empty, attributePosition)),
                null, default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ParameterCountMismatch, attributePosition)
                .WithExpected("2")
                .WithActual("0");
        }

        [Test]
        public void Compile_import_attribute_with_wrong_parameter_types()
        {
            var paramList = ImmutableList<LiteralSyntax>.Empty
                .Add(new IntegerLiteralSyntax(14, new TextPosition(0, 1, 2)))
                .Add(new BooleanLiteralSyntax(true, new TextPosition(0, 3, 4)));

            var syntax = new FunctionSyntax("MethodName", MakeType("bool"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty.Add(new AttributeSyntax("Import", paramList, default)),
                null, default);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ImportParameterNotValid, paramList[0].Position);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ImportParameterNotValid, paramList[1].Position);
        }

        [Test]
        public void Compile_import_and_entry_point_attributes_may_not_coexist()
        {
            var position = new TextPosition(17, 2, 3);
            var syntax = new FunctionSyntax("MethodName", MakeType("int32"),
                Visibility.Public, ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty
                    .Add(MakeImportAttribute("Method", "Library"))
                    .Add(MakeEntryPointAttribute()),
                null, position);
            var diagnostics = new TestingDiagnosticSink();
            var declarationProvider = new TestingSingleFileDeclarationProvider();

            var result = MethodDeclarationCompiler.Compile(syntax, "ns", "import.cle", 0, declarationProvider, diagnostics);

            Assert.That(result, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.EntryPointAndImportNotCompatible, position);
        }

        private static AttributeSyntax MakeEntryPointAttribute()
        {
            return new AttributeSyntax("EntryPoint", ImmutableList<LiteralSyntax>.Empty, default);
        }

        private static AttributeSyntax MakeImportAttribute(string name, string library)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var libraryBytes = Encoding.UTF8.GetBytes(library);

            var paramList = ImmutableList<LiteralSyntax>.Empty
                .Add(new StringLiteralSyntax(nameBytes, default))
                .Add(new StringLiteralSyntax(libraryBytes, default));

            return new AttributeSyntax("Import", paramList, default);
        }

        private static FunctionSyntax MakeParameterlessMethod(Visibility visibility,
            string returnType, TextPosition position)
        {
            return new FunctionSyntax("MethodName",
                new TypeNameSyntax(returnType, default),
                visibility,
                ImmutableList<ParameterDeclarationSyntax>.Empty,
                ImmutableList<AttributeSyntax>.Empty,
                new BlockSyntax(ImmutableList<StatementSyntax>.Empty, default),
                position);
        }

        private static ParameterDeclarationSyntax MakeParameter(string type, string name,
            TextPosition position) => new ParameterDeclarationSyntax(new TypeNameSyntax(type, position), name, position);

        private static TypeNameSyntax MakeType(string typeName) => new TypeNameSyntax(typeName, default);
    }
}
