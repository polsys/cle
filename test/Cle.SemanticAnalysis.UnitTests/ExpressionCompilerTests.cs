using System;
using System.Text;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class ExpressionCompilerTests
    {
        [Test]
        public void Integer_literal_stored_in_int32_succeeds()
        {
            var syntax = new IntegerLiteralSyntax(1234, default);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var variableMap = new ScopedVariableMap();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(local.InitialValue, Is.EqualTo(ConstantValue.SignedInteger(1234)));
        }

        [Test]
        public void Integer_literal_stored_in_bool_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IntegerLiteralSyntax(1234, position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var variableMap = new ScopedVariableMap();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("int32")
                .WithExpected("bool");
        }

        [Test]
        public void Integer_literal_that_is_too_large_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IntegerLiteralSyntax((ulong)int.MaxValue + 1, position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var variableMap = new ScopedVariableMap();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("uint32").WithExpected("int32");
        }

        [Test]
        public void Boolean_literal_stored_in_bool_succeeds()
        {
            var syntax = new BooleanLiteralSyntax(true, default);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var variableMap = new ScopedVariableMap();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(local.InitialValue, Is.EqualTo(ConstantValue.Bool(true)));
        }

        [Test]
        public void Variable_reference_returns_local_index_of_variable()
        {
            var syntax = new NamedValueSyntax("a", default);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", 0);
            method.AddLocal(SimpleType.Bool, ConstantValue.Bool(true));

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
        }

        [Test]
        public void Variable_reference_must_have_correct_type()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new NamedValueSyntax("a", position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", 0);
            method.AddLocal(SimpleType.Bool, ConstantValue.Bool(true));

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("bool").WithExpected("int32");
        }

        [Test]
        public void Variable_reference_must_refer_to_existent_variable()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new NamedValueSyntax("a", position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableNotFound, position).WithActual("a");
        }

        [TestCase("40 + 2", 42)]
        [TestCase("40 - 2", 38)]
        [TestCase("40 * 2", 80)]
        [TestCase("40 / 2", 20)]
        [TestCase("(40 + 2) / 2", 21)]
        [TestCase("40 + 2 / 2", 41)]
        [TestCase("41 / 2", 20)] // Rounded towards zero
        [TestCase("-41 / 2", -20)] // Rounded towards zero
        [TestCase("-40", -40)]
        [TestCase("-2147483648", -2147483648)] // Smallest expressible int32
        public void Int32_constant_expression_compiled_successfully(string expressionString, int expectedValue)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(method.Values[localIndex].InitialValue, Is.EqualTo(ConstantValue.SignedInteger(expectedValue)));
        }

        [TestCase("10 + a", Opcode.Add)]
        [TestCase("10 - a", Opcode.Subtract)]
        [TestCase("10 * a", Opcode.Multiply)]
        [TestCase("10 / a", Opcode.Divide)]
        public void Int32_non_constant_binary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, ConstantValue.Void()));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(2));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(builder.Instructions, Has.Exactly(1).Items);

            var instruction = builder.Instructions[0];
            Assert.That(instruction.Operation, Is.EqualTo(expectedOp));
            Assert.That(instruction.Left, Is.EqualTo(1));
            Assert.That(instruction.Right, Is.EqualTo(0));
            Assert.That(instruction.Destination, Is.EqualTo(2));
        }

        [TestCase("-a", Opcode.ArithmeticNegate)]
        public void Int32_non_constant_unary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, ConstantValue.Void()));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, variableMap, diagnostics);
            
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(1));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(builder.Instructions, Has.Exactly(1).Items);

            var instruction = builder.Instructions[0];
            Assert.That(instruction.Operation, Is.EqualTo(expectedOp));
            Assert.That(instruction.Left, Is.EqualTo(0));
            Assert.That(instruction.Destination, Is.EqualTo(1));
        }

        [TestCase("1 + true")]
        [TestCase("1 - true")]
        [TestCase("1 * true")]
        [TestCase("1 / true")]
        [TestCase("true + true")]
        [TestCase("-true")]
        public void Type_error_in_constant_expression(string expressionString)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                expressionSyntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, expressionSyntax.Position)
                .WithActual("bool").WithExpected("int32");
        }

        [TestCase("-(1 + true)")]
        [TestCase("(-true) + 2")]
        public void Failure_in_inner_expression_is_bubbled_up(string expressionString)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                expressionSyntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Not.Empty);
            Assert.That(localIndex, Is.EqualTo(-1));
        }

        [Test]
        public void Int32_division_by_zero_in_constant_expression_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new BinaryExpressionSyntax(BinaryOperation.Divide, 
                new IntegerLiteralSyntax(2ul, default), 
                new IntegerLiteralSyntax(0, default), 
                position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.DivisionByConstantZero, position);
        }

        [Test]
        public void Int32_division_by_constant_zero_in_non_constant_expression_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new BinaryExpressionSyntax(BinaryOperation.Divide,
                new NamedValueSyntax("a", default), 
                new IntegerLiteralSyntax(0, default),
                position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, ConstantValue.Void()));

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.DivisionByConstantZero, position);
        }

        [Test]
        public void Negated_int32_literal_that_is_too_small_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new UnaryExpressionSyntax(UnaryOperation.Minus,
                new IntegerLiteralSyntax(2_147_483_649, default), // One less than smallest int32
                position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, variableMap, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.IntegerConstantOutOfBounds, position);
        }

        private static ExpressionSyntax ParseExpression(string expressionString)
        {
            // A slightly nasty way of getting the single expression parse tree
            // Parser tests have a more direct way of parsing expressions but that is not available here
            var source = $"namespace Test; public int32 Fun() {{ return {expressionString}; }}";
            var sourceBytes = Encoding.UTF8.GetBytes(source).AsMemory();
            var diagnostics = new TestingDiagnosticSink();
            var parseTree = SyntaxParser.Parse(sourceBytes, "file.cle", diagnostics);
            var expressionSyntax = (parseTree?.Functions[0].Block.Statements[0] as ReturnStatementSyntax)?.ResultExpression;

            Assert.That(expressionSyntax, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            return expressionSyntax;
        }
    }
}
