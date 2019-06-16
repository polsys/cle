using Cle.Common;
using Cle.Parser.SyntaxTree;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class FunctionTests : SyntaxParserTestBase
    {
        [Test]
        public void Empty_global_private_void_function_is_correctly_read()
        {
            const string source = @"namespace Test;

private void Function()
{
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Visibility, Is.EqualTo(Visibility.Private));
            Assert.That(function.Name, Is.EqualTo("Function"));
            Assert.That(function.ReturnTypeName, Is.EqualTo("void"));
            Assert.That(function.Parameters, Is.Empty);

            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Position.Line, Is.EqualTo(4));
            Assert.That(function.Block.Position.ByteInLine, Is.EqualTo(0));
            Assert.That(function.Block.Statements, Is.Empty);
        }

        [Test]
        public void Empty_global_public_int32_function_is_correctly_read()
        {
            const string source = @"namespace Test;

public int32 Function2()
{
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Visibility, Is.EqualTo(Visibility.Public));
            Assert.That(function.Name, Is.EqualTo("Function2"));
            Assert.That(function.ReturnTypeName, Is.EqualTo("int32"));
        }

        [Test]
        public void Empty_global_internal_custom_typed_function_is_correctly_read()
        {
            const string source = @"namespace Test;

internal Other::Namespace::Type _fun()
{
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Visibility, Is.EqualTo(Visibility.Internal));
            Assert.That(function.Name, Is.EqualTo("_fun"));
            Assert.That(function.ReturnTypeName, Is.EqualTo("Other::Namespace::Type"));
        }

        [Test]
        public void Parameter_list_is_correctly_read()
        {
            const string source = @"namespace Test;

private int32 Function(int32 a, Custom::Type _second_param)
{
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Parameters, Has.Exactly(2).Items);
            Assert.That(function.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(function.Parameters[0].TypeName, Is.EqualTo("int32"));
            Assert.That(function.Parameters[0].Position.ByteInLine, Is.EqualTo(23));
            Assert.That(function.Parameters[1].Name, Is.EqualTo("_second_param"));
            Assert.That(function.Parameters[1].TypeName, Is.EqualTo("Custom::Type"));
            Assert.That(function.Parameters[1].Position.ByteInLine, Is.EqualTo(32));
        }

        [Test]
        public void Two_empty_functions_are_both_correctly_read()
        {
            const string source = @"namespace EmptyFunctions;

internal int32 Integer()
{
}

 public bool Boolean()
{
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(2).Items);

            var first = syntaxTree.Functions[0];
            Assert.That(first.Visibility, Is.EqualTo(Visibility.Internal));
            Assert.That(first.Name, Is.EqualTo("Integer"));
            Assert.That(first.ReturnTypeName, Is.EqualTo("int32"));
            Assert.That(first.Position.Line, Is.EqualTo(3));
            Assert.That(first.Position.ByteInLine, Is.EqualTo(0));

            var second = syntaxTree.Functions[1];
            Assert.That(second.Visibility, Is.EqualTo(Visibility.Public));
            Assert.That(second.Name, Is.EqualTo("Boolean"));
            Assert.That(second.ReturnTypeName, Is.EqualTo("bool"));
            Assert.That(second.Position.Line, Is.EqualTo(7));
            Assert.That(second.Position.ByteInLine, Is.EqualTo(1));
        }

        [Test]
        public void Function_with_no_body_is_correctly_read()
        {
            const string source = @"namespace ImportedMaybe;

public int32 Imported();";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            Assert.That(syntaxTree.Functions[0].Visibility, Is.EqualTo(Visibility.Public));
            Assert.That(syntaxTree.Functions[0].Name, Is.EqualTo("Imported"));
            Assert.That(syntaxTree.Functions[0].ReturnTypeName, Is.EqualTo("int32"));
            Assert.That(syntaxTree.Functions[0].Block, Is.Null);
        }

        [Test]
        public void Global_int32_function_with_const_return_is_parsed_correctly()
        {
            const string source = @"namespace Test;

internal int32 OneHundred()
{
    return 100;
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Statements, Has.Exactly(1).Items);

            var returnStatement = (ReturnStatementSyntax)function.Block.Statements[0];
            Assert.That(returnStatement.Position.Line, Is.EqualTo(5));
            Assert.That(returnStatement.Position.ByteInLine, Is.EqualTo(4));
            Assert.That(returnStatement.ResultExpression, Is.Not.Null);
            Assert.That(returnStatement.ResultExpression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)returnStatement.ResultExpression!).Value, Is.EqualTo(100));
        }

        [Test]
        public void Global_int32_function_with_expression_return_is_parsed_correctly()
        {
            const string source = @"namespace Test;

internal int32 OneHundred()
{
    return 2*50;
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Statements, Has.Exactly(1).Items);

            var returnStatement = (ReturnStatementSyntax)function.Block.Statements[0];
            Assert.That(returnStatement.Position.Line, Is.EqualTo(5));
            Assert.That(returnStatement.Position.ByteInLine, Is.EqualTo(4));
            Assert.That(returnStatement.ResultExpression, Is.Not.Null);
            Assert.That(returnStatement.ResultExpression, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)returnStatement.ResultExpression!).Operation,
                Is.EqualTo(BinaryOperation.Times));
        }

        [Test]
        public void Global_void_function_with_return_is_parsed_correctly()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    return;
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Statements, Has.Exactly(1).Items);

            var returnStatement = (ReturnStatementSyntax)function.Block.Statements[0];
            Assert.That(returnStatement.Position.Line, Is.EqualTo(5));
            Assert.That(returnStatement.Position.ByteInLine, Is.EqualTo(4));
            Assert.That(returnStatement.ResultExpression, Is.Null);
        }

        [Test]
        public void Global_void_function_with_nested_blocks_is_parsed_correctly()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    {
        {}
        return;
    }
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Statements, Has.Exactly(1).Items);

            var innerBlock = (BlockSyntax)function.Block.Statements[0];
            Assert.That(innerBlock.Statements, Has.Exactly(2).Items);
            Assert.That(innerBlock.Statements[0], Is.InstanceOf<BlockSyntax>());
            Assert.That(innerBlock.Statements[1], Is.InstanceOf<ReturnStatementSyntax>());
        }

        [Test]
        public void Standalone_function_call_is_parsed_correctly()
        {
            const string source = @"namespace Test;

internal void DoSomething()
{
    DoSomethingInternal(42, GetParameter());
}";
            var syntaxTree = ParseSourceWithoutDiagnostics(source);
            Assert.That(syntaxTree.Functions, Has.Exactly(1).Items);

            var function = syntaxTree.Functions[0];
            Assert.That(function.Block, Is.Not.Null);
            Assert.That(function.Block!.Statements, Has.Exactly(1).Items);

            var call = (FunctionCallStatementSyntax)function.Block.Statements[0];
            Assert.That(call.Call.Function, Is.EqualTo("DoSomethingInternal"));
            Assert.That(call.Call.Parameters, Has.Exactly(2).Items);
            Assert.That(call.Call.Parameters[0], Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(call.Call.Parameters[1], Is.InstanceOf<FunctionCallSyntax>());
        }

        [Test]
        public void Empty_global_function_without_visibility_fails()
        {
            const string source = @"namespace Test;

int32 Function()
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSourceFileItem, 3, 0);
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_name_fails()
        {
            const string source = @"namespace Test;

public int32()
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedFunctionName, 3, 12).WithActual("(");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [TestCase("_")]
        [TestCase("Test::Function")]
        public void Empty_global_function_with_invalid_name_fails(string name)
        {
            var source = $@"namespace Test;

public int32 {name}(){{}}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidFunctionName, 3, 13).WithNonNullActual();
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_invalid_type_name_fails()
        {
            const string source = @"namespace Test;

public ::Test::Type function()
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidTypeName, 3, 7).WithActual("::Test::Type");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_type_and_name_fails()
        {
            const string source = @"namespace Test;

public()
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedType, 3, 6).WithActual("(");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_parameter_list_fails()
        {
            const string source = @"namespace Test;

public int32 Fun
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedParameterList, 4, 0).WithActual("{");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_close_paren_in_parameter_list_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedParameterDeclaration, 4, 0).WithActual("{");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_close_paren_after_parameter_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(int32 param1
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingParen, 4, 0).WithActual("{");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_leading_comma_in_parameter_list_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(,int32 second)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedParameterDeclaration, 3, 17).WithActual(",");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_extra_comma_in_parameter_list_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(int32 a,)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedParameterDeclaration, 3, 25).WithActual(")");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_no_parameter_name_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(int32)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedParameterName, 3, 22).WithActual(")");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_reserved_parameter_name_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(int32 int32)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 3, 23).WithActual("int32");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_invalid_parameter_name_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(int32 no::namespaces)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 3, 23).WithActual("no::namespaces");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_with_invalid_parameter_type_fails()
        {
            const string source = @"namespace Test;

public int32 Fun(Invalid::123 a)
{
}";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidTypeName, 3, 17).WithActual("Invalid::123");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_body_or_semicolon_fails()
        {
            const string source = @"namespace Test;

public int32 Fun()";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedMethodBody, 3, 18).WithActual("");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Empty_global_function_without_closing_brace_in_body_fails()
        {
            const string source = @"namespace Test;

public int32 Fun() {";
            var syntaxTree = ParseSource(source, out var diagnostics);
            
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingBrace, 3, 20).WithActual("");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Statement_expected_in_block()
        {
            const string source = @"namespace Test;

internal void Fail()
{
    42
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedStatement, 5, 4).WithActual("42");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Statement_expected_in_nested_block()
        {
            const string source = @"namespace Test;

internal void Fail()
{
    {42}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedStatement, 5, 5).WithActual("42");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Void_return_without_semicolon_fails()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    return
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            // Actually this fails because an expression could not be parsed
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 6, 0).WithActual("}");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Int32_return_without_semicolon_fails()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    return 100
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, 6, 0).WithActual("}");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Int32_return_without_expression_fails()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    return {100}
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 5, 11).WithActual("{");
            Assert.That(syntaxTree, Is.Null);
        }

        [Test]
        public void Function_call_with_invalid_parameter_fails()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    DoNothing(if)
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 5, 14).WithActual("if");
            Assert.That(syntaxTree, Is.Null);
        }
        
        [Test]
        public void Function_call_without_semicolon_fails()
        {
            const string source = @"namespace Test;

internal void DoNothing()
{
    DoNothing()
}";
            var syntaxTree = ParseSource(source, out var diagnostics);

            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedSemicolon, 6, 0).WithActual("}");
            Assert.That(syntaxTree, Is.Null);
        }
    }
}
