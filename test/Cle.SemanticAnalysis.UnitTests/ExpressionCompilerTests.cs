using System;
using System.Collections.Generic;
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
        // NOTE: Method calls, even in expression context, are tested in MethodCompilerTests.CallTests.
        // This is because of the more complex IR emitted.

        [Test]
        public void Integer_literal_stored_in_int32_succeeds()
        {
            var syntax = new IntegerLiteralSyntax(1234, default);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var nameResolver = new TestingResolver(new ScopedVariableMap());
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Int32));

            AssertSingleLoad(builder, localIndex, 1234);
        }

        [Test]
        public void Integer_literal_stored_in_bool_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IntegerLiteralSyntax(1234, position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var nameResolver = new TestingResolver(new ScopedVariableMap());
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, nameResolver, diagnostics);

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
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var nameResolver = new TestingResolver(new ScopedVariableMap());
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("uint32").WithExpected("int32");
        }

        [Test]
        public void Boolean_literal_stored_in_bool_succeeds()
        {
            var syntax = new BooleanLiteralSyntax(true, default);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var nameResolver = new TestingResolver(new ScopedVariableMap());
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Bool));
            AssertSingleLoad(builder, localIndex, true);
        }

        [Test]
        public void Variable_reference_returns_local_index_of_variable()
        {
            var syntax = new IdentifierSyntax("a", default);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", 0);
            method.AddLocal(SimpleType.Bool, LocalFlags.None);

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
        }

        [Test]
        public void Variable_reference_must_have_correct_type()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IdentifierSyntax("a", position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", 0);
            method.AddLocal(SimpleType.Bool, LocalFlags.None);

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("bool").WithExpected("int32");
        }

        [Test]
        public void Variable_reference_must_refer_to_existent_variable()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IdentifierSyntax("a", position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, new TestingResolver(variableMap), diagnostics);

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
        [TestCase("40 % 3", 1)]
        [TestCase("-40 % 3", -1)]
        [TestCase("40 % -3", 1)]
        [TestCase("5 & 9", 1)]
        [TestCase("5 | 9", 13)]
        [TestCase("5 ^ 9", 12)]
        [TestCase("5 << 1", 10)]
        [TestCase("5 >> 1", 2)]
        [TestCase("5 << 32", 5)] // Shift amount is masked
        [TestCase("5 << -1", -2147483648)] // Shift amount is masked (-1 & 0x1F = 31, so the result is 0x80000000)
        [TestCase("~1", -2)]
        public void Int32_constant_expression_compiled_successfully(string expressionString, int expectedValue)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            AssertSingleLoad(builder, localIndex, expectedValue);
        }

        [TestCase("true & true", true)]
        [TestCase("true & false", false)]
        [TestCase("true | false", true)]
        [TestCase("false | false", false)]
        [TestCase("false ^ false", false)]
        [TestCase("false ^ true", true)]
        [TestCase("!true", false)]
        [TestCase("!!true", true)]
        public void Bool_constant_expression_compiled_successfully(string expressionString, bool expectedValue)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, nameResolver, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
            AssertSingleLoad(builder, localIndex, expectedValue);
        }

        [TestCase("1 < 2", true)]
        [TestCase("1 <= 2", true)]
        [TestCase("2 <= 2", true)]
        [TestCase("2 < 2", false)]
        [TestCase("2 > 2", false)]
        [TestCase("2 >= 2", true)]
        [TestCase("3 >= 2", true)]
        [TestCase("3 > 2", true)]
        [TestCase("-3 == 2", false)]
        [TestCase("-3 != 2", true)]
        [TestCase("2 == 2", true)]
        [TestCase("2 != 2", false)]
        [TestCase("true == true", true)]
        [TestCase("true == false", false)]
        [TestCase("true != true", false)]
        [TestCase("true != false", true)]
        public void Constant_comparison_compiled_successfully(string expressionString, bool expectedValue)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, nameResolver, diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
            AssertSingleLoad(builder, localIndex, expectedValue);
        }

        [TestCase("10 + a", Opcode.Add)]
        [TestCase("10 - a", Opcode.Subtract)]
        [TestCase("10 * a", Opcode.Multiply)]
        [TestCase("10 / a", Opcode.Divide)]
        [TestCase("10 % a", Opcode.Modulo)]
        [TestCase("10 & a", Opcode.BitwiseAnd)]
        [TestCase("10 | a", Opcode.BitwiseOr)]
        [TestCase("10 ^ a", Opcode.BitwiseXor)]
        [TestCase("10 << a", Opcode.ShiftLeft)]
        [TestCase("10 >> a", Opcode.ShiftRight)]
        public void Int32_non_constant_binary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(2));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(builder.Instructions, Has.Exactly(2).Items);

            var load = builder.Instructions[0];
            Assert.That(load.Operation, Is.EqualTo(Opcode.Load));
            Assert.That(load.Left, Is.EqualTo(10));
            Assert.That(load.Right, Is.EqualTo(0));
            Assert.That(load.Destination, Is.EqualTo(1));

            var action = builder.Instructions[1];
            Assert.That(action.Operation, Is.EqualTo(expectedOp));
            Assert.That(action.Left, Is.EqualTo(1));
            Assert.That(action.Right, Is.EqualTo(0));
            Assert.That(action.Destination, Is.EqualTo(2));
        }

        [TestCase("true & a", Opcode.BitwiseAnd)]
        [TestCase("true | a", Opcode.BitwiseOr)]
        [TestCase("true ^ a", Opcode.BitwiseXor)]
        public void Bool_non_constant_binary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Bool, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(2));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(builder.Instructions, Has.Exactly(2).Items);

            var load = builder.Instructions[0];
            Assert.That(load.Operation, Is.EqualTo(Opcode.Load));
            Assert.That(load.Left, Is.EqualTo(1));
            Assert.That(load.Right, Is.EqualTo(0));
            Assert.That(load.Destination, Is.EqualTo(1));

            var action = builder.Instructions[1];
            Assert.That(action.Operation, Is.EqualTo(expectedOp));
            Assert.That(action.Left, Is.EqualTo(1));
            Assert.That(action.Right, Is.EqualTo(0));
            Assert.That(action.Destination, Is.EqualTo(2));
        }

        [TestCase("a < b", Opcode.Less, false)]
        [TestCase("a > b", Opcode.Less, true)]
        [TestCase("a <= b", Opcode.LessOrEqual, false)]
        [TestCase("a >= b", Opcode.LessOrEqual, true)]
        [TestCase("a == b", Opcode.Equal, false)]
        public void Non_constant_comparison_compiled_successfully(string expressionString,
            Opcode expectedOp, bool shouldBeFlipped)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, LocalFlags.None));
            variableMap.TryAddVariable("b", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(2));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(builder.Instructions, Has.Exactly(1).Items);

            var instruction = builder.Instructions[0];
            Assert.That(instruction.Operation, Is.EqualTo(expectedOp));
            if (shouldBeFlipped)
            {
                Assert.That(instruction.Left, Is.EqualTo(1));
                Assert.That(instruction.Right, Is.EqualTo(0));
            }
            else
            {
                Assert.That(instruction.Left, Is.EqualTo(0));
                Assert.That(instruction.Right, Is.EqualTo(1));
            }
            Assert.That(instruction.Destination, Is.EqualTo(2));
        }

        [Test]
        public void Non_constant_inequality_compiled_successfully()
        {
            var expressionSyntax = ParseExpression("a != b");
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, LocalFlags.None));
            variableMap.TryAddVariable("b", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(3));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(builder.Instructions, Has.Exactly(2).Items);
            
            // a != b is compiled as !(a == b)
            Assert.That(builder.Instructions[0], Is.EqualTo(new Instruction(Opcode.Equal, 0, 1, 2)));
            Assert.That(builder.Instructions[1], Is.EqualTo(new Instruction(Opcode.BitwiseNot, 2, 0, 3)));
        }

        [TestCase("-a", Opcode.ArithmeticNegate)]
        [TestCase("~a", Opcode.BitwiseNot)]
        public void Int32_non_constant_unary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Int32, method, builder, new TestingResolver(variableMap), diagnostics);
            
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(1));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(builder.Instructions, Has.Exactly(1).Items);

            var instruction = builder.Instructions[0];
            Assert.That(instruction.Operation, Is.EqualTo(expectedOp));
            Assert.That(instruction.Left, Is.EqualTo(0));
            Assert.That(instruction.Destination, Is.EqualTo(1));
        }

        [TestCase("!a", Opcode.BitwiseNot)]
        public void Bool_non_constant_unary_expression_compiled_successfully(string expressionString, Opcode expectedOp)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Bool, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(expressionSyntax,
                SimpleType.Bool, method, builder, new TestingResolver(variableMap), diagnostics);
            
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(localIndex, Is.EqualTo(1));
            Assert.That(method.Values[localIndex].Type, Is.EqualTo(SimpleType.Bool));
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
        [TestCase("1 % true")]
        [TestCase("1 & true")]
        [TestCase("1 | true")]
        [TestCase("1 ^ true")]
        [TestCase("1 << true")]
        [TestCase("1 >> true")]
        [TestCase("1 < true")]
        [TestCase("1 <= true")]
        [TestCase("1 > true")]
        [TestCase("1 >= true")]
        [TestCase("1 == true")]
        [TestCase("1 != true")]
        public void Type_error_in_constant_expression(string expressionString)
        {
            var localIndex = TryCompileConstantExpression(expressionString, SimpleType.Int32,
                out var diagnostics, out var position);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("bool").WithExpected("int32");
        }

        [TestCase("true + true", "+", "bool")]
        [TestCase("-true", "-", "bool")]
        [TestCase("~true", "~", "bool")]
        [TestCase("!2", "!", "int32")]
        [TestCase("true < false", "<", "bool")]
        [TestCase("true <= false", "<=", "bool")]
        [TestCase("true >= false", ">=", "bool")]
        [TestCase("true > false", ">", "bool")]
        public void Operator_not_defined_for_type_in_constant(string expressionString, string operatorName, string typeName)
        {
            var localIndex = TryCompileConstantExpression(expressionString, SimpleType.Int32,
                out var diagnostics, out var position);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.OperatorNotDefined, position)
                .WithActual(operatorName).WithExpected(typeName);
        }

        [TestCase("b + b", "+", "bool")]
        [TestCase("-b", "-", "bool")]
        [TestCase("~b", "~", "bool")]
        [TestCase("!i", "!", "int32")]
        [TestCase("b < b", "<", "bool")]
        [TestCase("b <= b", "<=", "bool")]
        [TestCase("b >= b", ">=", "bool")]
        [TestCase("b > b", ">", "bool")]
        public void Operator_not_defined_for_type_in_non_constant(string expressionString, string operatorName, string typeName)
        {
            var expressionSyntax = ParseExpression(expressionString);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("b", method.AddLocal(SimpleType.Bool, LocalFlags.None));
            variableMap.TryAddVariable("i", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(
                expressionSyntax, SimpleType.Void, method, builder, new TestingResolver(variableMap), diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.OperatorNotDefined, expressionSyntax.Position)
                .WithActual(operatorName).WithExpected(typeName);
        }

        [TestCase("-(1 + true)")]
        [TestCase("(-true) + 2")]
        public void Failure_in_inner_expression_is_bubbled_up(string expressionString)
        {
            var localIndex = TryCompileConstantExpression(expressionString, SimpleType.Int32, out var diagnostics, out _);

            Assert.That(diagnostics.Diagnostics, Is.Not.Empty);
            Assert.That(localIndex, Is.EqualTo(-1));
        }

        [TestCase("true && true", "&&")]
        [TestCase("true || true", "||")]
        public void Short_circuiting_boolean_operators_are_disabled(string source, string operatorName)
        {
            var localIndex = TryCompileConstantExpression(source, SimpleType.Bool, out var diagnostics, out var position);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.OperatorNotDefined, position)
                .WithActual(operatorName).WithExpected(SimpleType.Bool.TypeName);
        }

        [TestCase(BinaryOperation.Divide)]
        [TestCase(BinaryOperation.Modulo)]
        public void Int32_division_by_zero_in_constant_expression_fails(BinaryOperation op)
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new BinaryExpressionSyntax(op, 
                new IntegerLiteralSyntax(2ul, default), 
                new IntegerLiteralSyntax(0, default), 
                position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.DivisionByConstantZero, position);
        }

        [Test]
        public void Invalid_remainder_in_constant_expression_fails()
        {
            // int.MinValue % -1 throws OverflowException in C#
            var position = new TextPosition(10, 3, 4);
            var syntax = new BinaryExpressionSyntax(BinaryOperation.Modulo, 
                new UnaryExpressionSyntax(UnaryOperation.Minus, new IntegerLiteralSyntax(int.MaxValue + 1ul, default), default),
                new UnaryExpressionSyntax(UnaryOperation.Minus, new IntegerLiteralSyntax(1, default), default),
                position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.IntegerConstantOutOfBounds, position);
        }

        [TestCase(BinaryOperation.Divide)]
        [TestCase(BinaryOperation.Modulo)]
        public void Int32_division_by_constant_zero_in_non_constant_expression_fails(BinaryOperation op)
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new BinaryExpressionSyntax(op,
                new IdentifierSyntax("a", default), 
                new IntegerLiteralSyntax(0, default),
                position);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var variableMap = new ScopedVariableMap();
            variableMap.PushScope();
            variableMap.TryAddVariable("a", method.AddLocal(SimpleType.Int32, LocalFlags.None));

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, new TestingResolver(variableMap), diagnostics);

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
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, nameResolver, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.IntegerConstantOutOfBounds, position);
        }

        private void AssertSingleLoad(BasicBlockBuilder builder, int localIndex, int expected)
        {
            Assert.That(builder.Instructions, Has.Exactly(1).Items);
            Assert.That(builder.Instructions[0].Operation, Is.EqualTo(Opcode.Load));
            Assert.That(builder.Instructions[0].Destination, Is.EqualTo(localIndex));
            Assert.That(builder.Instructions[0].Left, Is.EqualTo((ulong)expected));
        }

        private void AssertSingleLoad(BasicBlockBuilder builder, int localIndex, bool expected)
        {
            Assert.That(builder.Instructions, Has.Exactly(1).Items);
            Assert.That(builder.Instructions[0].Operation, Is.EqualTo(Opcode.Load));
            Assert.That(builder.Instructions[0].Destination, Is.EqualTo(localIndex));
            Assert.That(builder.Instructions[0].Left, Is.EqualTo(expected ? 1 : 0));
        }

        private static int TryCompileConstantExpression(string source, SimpleType expectedType,
            out TestingDiagnosticSink diagnostics, out TextPosition expressionPosition)
        {
            var expressionSyntax = ParseExpression(source);
            var method = new CompiledMethod("Test::Method");
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            diagnostics = new TestingDiagnosticSink();
            var nameResolver = new TestingResolver(new ScopedVariableMap());

            expressionPosition = expressionSyntax.Position;
            return ExpressionCompiler.TryCompileExpression(expressionSyntax, 
                expectedType, method, builder, nameResolver, diagnostics);
        }

        private static ExpressionSyntax ParseExpression(string expressionString)
        {
            // A slightly nasty way of getting the single expression parse tree
            // Parser tests have a more direct way of parsing expressions but that is not available here
            var source = $"namespace Test; public int32 Fun() {{ return {expressionString}; }}";
            var sourceBytes = Encoding.UTF8.GetBytes(source).AsMemory();
            var diagnostics = new TestingDiagnosticSink();
            var parseTree = SyntaxParser.Parse(sourceBytes, "file.cle", diagnostics);
            var expressionSyntax = (parseTree?.Functions[0].Block!.Statements[0] as ReturnStatementSyntax)?.ResultExpression;

            Assert.That(expressionSyntax, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            return expressionSyntax!;
        }

        private class TestingResolver : INameResolver
        {
            private readonly ScopedVariableMap _variableMap;

            public TestingResolver(ScopedVariableMap variableMap)
            {
                _variableMap = variableMap;
            }

            public IReadOnlyList<MethodDeclaration> ResolveMethod(string name)
            {
                throw new NotImplementedException();
            }

            public bool TryResolveVariable(string name, out int localIndex)
            {
                return _variableMap.TryGetVariable(name, out localIndex);
            }
        }
    }
}
