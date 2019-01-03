using Cle.Common;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Parser.UnitTests.SyntaxParserTests
{
    public class ExpressionTests : SyntaxParserTestBase
    {
        [TestCase("0", 0ul)]
        [TestCase("1234567890", 1234567890ul)]
        [TestCase("007", 7ul)]
        [TestCase("18446744073709551615", ulong.MaxValue)]
        public void Valid_integer_literals(string source, ulong expected)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);
            Assert.That(expression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)expression).Value, Is.EqualTo(expected));
        }

        [TestCase("0xDEAD")]
        [TestCase("1`234`567`890")]
        [TestCase("18446744073709551616")] // Too big to fit in uint64
        public void Invalid_integer_literals(string source)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidNumericLiteral, 1, 0);
        }

        [TestCase("true", true)]
        [TestCase("false", false)]
        public void Valid_Boolean_literals(string source, bool expected)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);
            Assert.That(expression, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)expression).Value, Is.EqualTo(expected));
        }

        [TestCase("i")]
        [TestCase("Somewhere::Something")]
        public void Variable_or_constant_name_alone(string name)
        {
            var parser = GetParserInstance(name, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var reference = (NamedValueSyntax)expression;
            Assert.That(reference.Name, Is.EqualTo(name));
        }

        [TestCase("_")]
        [TestCase("int32")]
        [TestCase("Somewhere:Something")]
        public void Variable_name_must_be_valid(string name)
        {
            var parser = GetParserInstance(name, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.InvalidVariableName, 1, 0).WithActual(name);
        }

        [TestCase("-123", UnaryOperation.Minus)]
        [TestCase("~123", UnaryOperation.Complement)]
        public void Unary_integer_operation_is_parsed_correctly(string source, UnaryOperation expectedOp)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var unary = (UnaryExpressionSyntax)expression;
            Assert.That(unary.Operation, Is.EqualTo(expectedOp));
            Assert.That(unary.InnerExpression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)unary.InnerExpression).Value, Is.EqualTo(123));
        }

        [TestCase("!true", UnaryOperation.Negation)]
        public void Unary_boolean_operation_is_parsed_correctly(string source, UnaryOperation expectedOp)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var unary = (UnaryExpressionSyntax)expression;
            Assert.That(unary.Operation, Is.EqualTo(expectedOp));
            Assert.That(unary.InnerExpression, Is.InstanceOf<BooleanLiteralSyntax>());
            Assert.That(((BooleanLiteralSyntax)unary.InnerExpression).Value, Is.EqualTo(true));
        }

        [Test]
        public void Unary_minus_can_be_repeated()
        {
            var parser = GetParserInstance("--123", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var unary = (UnaryExpressionSyntax)expression;
            Assert.That(unary.Operation, Is.EqualTo(UnaryOperation.Minus));

            var innerUnary = (UnaryExpressionSyntax)unary.InnerExpression;

            Assert.That(innerUnary.InnerExpression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)innerUnary.InnerExpression).Value, Is.EqualTo(123));
        }

        [TestCase("1 + 2 + 3", BinaryOperation.Plus)]
        [TestCase("1 - 2 - 3", BinaryOperation.Minus)]
        [TestCase("1 * 2 * 3", BinaryOperation.Times)]
        [TestCase("1 / 2 / 3", BinaryOperation.Divide)]
        [TestCase("1 % 2 % 3", BinaryOperation.Modulo)]
        [TestCase("1 & 2 & 3", BinaryOperation.And)]
        [TestCase("1 && 2 && 3", BinaryOperation.ShortCircuitAnd)]
        [TestCase("1 | 2 | 3", BinaryOperation.Or)]
        [TestCase("1 || 2 || 3", BinaryOperation.ShortCircuitOr)]
        [TestCase("1 ^ 2 ^ 3", BinaryOperation.Xor)]
        [TestCase("1 == 2 == 3", BinaryOperation.Equal)]
        [TestCase("(1 + 2 + 3)", BinaryOperation.Plus)] // Parens outside the expression have no effect
        [TestCase("(1 ^ 2 ^ 3)", BinaryOperation.Xor)]
        public void Binary_operation_is_left_associative([NotNull] string source, BinaryOperation operation)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(operation));

            var left = (BinaryExpressionSyntax)binary.Left;
            Assert.That(left.Operation, Is.EqualTo(operation));
            Assert.That(left.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Left).Value, Is.EqualTo(1));
            Assert.That(left.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Right).Value, Is.EqualTo(2));

            Assert.That(binary.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Right).Value, Is.EqualTo(3));
        }

        [Test]
        public void Multiplication_has_correct_precedence_on_left()
        {
            var parser = GetParserInstance("1 * 2 + 3", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Plus));

            var left = (BinaryExpressionSyntax)binary.Left;
            Assert.That(left.Operation, Is.EqualTo(BinaryOperation.Times));
            Assert.That(left.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Left).Value, Is.EqualTo(1));
            Assert.That(left.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Right).Value, Is.EqualTo(2));

            Assert.That(binary.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Right).Value, Is.EqualTo(3));
        }

        [Test]
        public void Multiplication_has_correct_precedence_on_right()
        {
            var parser = GetParserInstance("1 + 2 * 3", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Plus));

            Assert.That(binary.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Left).Value, Is.EqualTo(1));

            var right = (BinaryExpressionSyntax)binary.Right;
            Assert.That(right.Operation, Is.EqualTo(BinaryOperation.Times));
            Assert.That(right.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Left).Value, Is.EqualTo(2));
            Assert.That(right.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Right).Value, Is.EqualTo(3));
        }

        [Test]
        public void Binary_minus_and_division_are_parsed_correctly()
        {
            var parser = GetParserInstance("3 - 4/2", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Minus));

            Assert.That(binary.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Left).Value, Is.EqualTo(3));

            var right = (BinaryExpressionSyntax)binary.Right;
            Assert.That(right.Operation, Is.EqualTo(BinaryOperation.Divide));
            Assert.That(right.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Left).Value, Is.EqualTo(4));
            Assert.That(right.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Right).Value, Is.EqualTo(2));
        }

        [Test]
        public void Binary_and_unary_minus_combined()
        {
            var parser = GetParserInstance("1 - -2", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Minus));

            Assert.That(binary.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Left).Value, Is.EqualTo(1));

            var right = (UnaryExpressionSyntax)binary.Right;
            Assert.That(right.Operation, Is.EqualTo(UnaryOperation.Minus));
            Assert.That(right.InnerExpression, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.InnerExpression).Value, Is.EqualTo(2));
        }

        [Test]
        public void Variable_and_literal_in_binary_expression()
        {
            var parser = GetParserInstance("a + 1", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Plus));

            Assert.That(binary.Left, Is.InstanceOf<NamedValueSyntax>());
            Assert.That(((NamedValueSyntax)binary.Left).Name, Is.EqualTo("a"));

            Assert.That(binary.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Right).Value, Is.EqualTo(1));
        }

        [Test]
        public void Binary_and_double_unary_minus_combined()
        {
            // This is horrible misuse of the language.
            // It also will probably be the first thing to break when doing careless modifications.
            var parser = GetParserInstance("1---2", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Minus));

            Assert.That(binary.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Left).Value, Is.EqualTo(1));

            var right = (UnaryExpressionSyntax)binary.Right;
            Assert.That(right.Operation, Is.EqualTo(UnaryOperation.Minus));
            Assert.That(right.InnerExpression, Is.InstanceOf<UnaryExpressionSyntax>());

            var innerUnary = (UnaryExpressionSyntax)right.InnerExpression;
            Assert.That(innerUnary.Operation, Is.EqualTo(UnaryOperation.Minus));
            Assert.That(((IntegerLiteralSyntax)innerUnary.InnerExpression).Value, Is.EqualTo(2));
        }

        [Test]
        public void Parens_modify_precedence()
        {
            var parser = GetParserInstance("(1 + 2) * 3", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Times));

            var left = (BinaryExpressionSyntax)binary.Left;
            Assert.That(left.Operation, Is.EqualTo(BinaryOperation.Plus));
            Assert.That(left.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Left).Value, Is.EqualTo(1));
            Assert.That(left.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)left.Right).Value, Is.EqualTo(2));

            Assert.That(binary.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Right).Value, Is.EqualTo(3));
        }

        [Test]
        public void Parens_modify_associativity()
        {
            var parser = GetParserInstance("1 + (2 + 3)", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.Plus));

            Assert.That(binary.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)binary.Left).Value, Is.EqualTo(1));

            var right = (BinaryExpressionSyntax)binary.Right;
            Assert.That(right.Operation, Is.EqualTo(BinaryOperation.Plus));
            Assert.That(right.Left, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Left).Value, Is.EqualTo(2));
            Assert.That(right.Right, Is.InstanceOf<IntegerLiteralSyntax>());
            Assert.That(((IntegerLiteralSyntax)right.Right).Value, Is.EqualTo(3));
        }

        [TestCase("(-1 + 2) << 2 < 3 * 4 % 5", BinaryOperation.LessThan)]
        [TestCase("(-1 + 2) << 2 <= 3 * 4 % 5", BinaryOperation.LessThanOrEqual)]
        [TestCase("(-1 + 2) << 2 > 3 * 4 % 5", BinaryOperation.GreaterThan)]
        [TestCase("(-1 + 2) << 2 >= 3 * 4 % 5", BinaryOperation.GreaterThanOrEqual)]
        [TestCase("(-1 + 2) << 2 == 3 * 4 % 5", BinaryOperation.Equal)]
        [TestCase("(-1 + 2) << 2 != 3 * 4 % 5", BinaryOperation.NotEqual)]
        public void Relational_operators_have_lower_precedence_than_arithmetic_or_shift(string source, BinaryOperation expected)
        {
            var parser = GetParserInstance(source, out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(expected));

            Assert.That(binary.Left, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Left).Operation, Is.EqualTo(BinaryOperation.ShiftLeft));

            Assert.That(binary.Right, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Right).Operation, Is.EqualTo(BinaryOperation.Modulo));
        }
        
        [Test]
        public void Logical_operators_have_lower_precedence_than_relational()
        {
            var parser = GetParserInstance("1 == 1 && 2 != 3", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.ShortCircuitAnd));

            Assert.That(binary.Left, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Left).Operation, Is.EqualTo(BinaryOperation.Equal));

            Assert.That(binary.Right, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Right).Operation, Is.EqualTo(BinaryOperation.NotEqual));
        }

        [Test]
        public void Shift_operator_has_lower_precedence_than_arithmetic()
        {
            var parser = GetParserInstance("2 + 2 << 2 - 1", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(returnValue, Is.True);
            Assert.That(expression, Is.Not.Null);

            var binary = (BinaryExpressionSyntax)expression;
            Assert.That(binary.Operation, Is.EqualTo(BinaryOperation.ShiftLeft));

            Assert.That(binary.Left, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Left).Operation, Is.EqualTo(BinaryOperation.Plus));

            Assert.That(binary.Right, Is.InstanceOf<BinaryExpressionSyntax>());
            Assert.That(((BinaryExpressionSyntax)binary.Right).Operation, Is.EqualTo(BinaryOperation.Minus));
        }

        [Test]
        public void Binary_plus_fails_if_right_side_is_not_term()
        {
            var parser = GetParserInstance("1 + ;", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 1, 4).WithActual(";");
        }

        [Test]
        public void Unary_minus_fails_if_right_side_is_not_factor()
        {
            var parser = GetParserInstance("-;", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 1, 1).WithActual(";");
        }

        [Test]
        public void Binary_multiplication_fails_if_right_side_is_not_term()
        {
            var parser = GetParserInstance("1 * ;", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 1, 4).WithActual(";");
        }

        [Test]
        public void Parens_must_be_closed()
        {
            var parser = GetParserInstance("(1 + 2;", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedClosingParen, 1, 6).WithActual(";");
        }

        [Test]
        public void Parens_must_have_expression_inside()
        {
            var parser = GetParserInstance("(;", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 1, 1).WithActual(";");
        }

        [Test]
        public void Right_hand_operand_must_be_valid()
        {
            // This test should exercise a failure path on all expression parsing levels
            var parser = GetParserInstance("true ^ 2 != 2 << 1 + 2 * -if", out var diagnostics);

            var returnValue = parser.TryParseExpression(out var expression);

            Assert.That(returnValue, Is.False);
            Assert.That(expression, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ExpectedExpression, 1, 26).WithActual("if");
        }
    }
}
