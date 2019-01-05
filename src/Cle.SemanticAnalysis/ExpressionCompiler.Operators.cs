using System;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;

namespace Cle.SemanticAnalysis
{
    internal static partial class ExpressionCompiler
    {
        // This file contains information about operators for built-in types,
        // operator names and compile-time evaluation for unary and binary operators.
        // There is no real compilation logic in here.

        private static Opcode GetUnaryOpcode(UnaryOperation operation)
        {
            switch (operation)
            {
                case UnaryOperation.Minus:
                    return Opcode.ArithmeticNegate;
                case UnaryOperation.Complement:
                    return Opcode.BitwiseNot;
                case UnaryOperation.Negation:
                    return Opcode.BitwiseNot;
                default:
                    throw new NotImplementedException("Unimplemented unary expression");
            }
        }

        private static long EvaluateConstantUnary(UnaryOperation operation, long value)
        {
            switch (operation)
            {
                case UnaryOperation.Minus:
                    return -value;
                case UnaryOperation.Complement:
                    return ~value;
                default:
                    throw new NotImplementedException("Unimplemented unary integer expression");
            }
        }

        private static bool EvaluateConstantUnary(UnaryOperation operation, bool value)
        {
            switch (operation)
            {
                case UnaryOperation.Negation:
                    return !value;
                default:
                    throw new NotImplementedException("Unimplemented unary Boolean expression");
            }
        }

        private static bool IsIntegerUnary(UnaryOperation operation)
        {
            // TODO: Consider whether this information should live somewhere else entirely
            //       Maybe refactor this when implementing user-defined operators?
            switch (operation)
            {
                case UnaryOperation.Minus:
                case UnaryOperation.Complement:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetOperatorName(UnaryOperation operation)
        {
            switch (operation)
            {
                case UnaryOperation.Minus:
                    return "-";
                case UnaryOperation.Complement:
                    return "~";
                case UnaryOperation.Negation:
                    return "!";
                default:
                    throw new NotImplementedException("Unimplemented unary expression");
            }
        }

        private static Opcode GetBinaryOpcode(BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.Plus:
                    return Opcode.Add;
                case BinaryOperation.Minus:
                    return Opcode.Subtract;
                case BinaryOperation.Times:
                    return Opcode.Multiply;
                case BinaryOperation.Divide:
                    return Opcode.Divide;
                case BinaryOperation.Modulo:
                    return Opcode.Modulo;
                case BinaryOperation.ShiftLeft:
                    return Opcode.ShiftLeft;
                case BinaryOperation.ShiftRight:
                    return Opcode.ShiftRight;
                case BinaryOperation.And:
                    return Opcode.BitwiseAnd;
                case BinaryOperation.Or:
                    return Opcode.BitwiseOr;
                case BinaryOperation.Xor:
                    return Opcode.BitwiseXor;
                case BinaryOperation.LessThan:
                    return Opcode.Less;
                case BinaryOperation.LessThanOrEqual:
                    return Opcode.LessOrEqual;
                case BinaryOperation.Equal:
                    return Opcode.Equal;
                default:
                    throw new NotImplementedException("Unimplemented binary expression");
            }
        }

        private static long EvaluateConstantBinary(BinaryOperation operation, long left, long right)
        {
            switch (operation)
            {
                case BinaryOperation.Plus:
                    return left + right;
                case BinaryOperation.Minus:
                    return left - right;
                case BinaryOperation.Times:
                    return left * right;
                case BinaryOperation.Divide:
                    return left / right;
                case BinaryOperation.Modulo:
                    return left % right;
                case BinaryOperation.ShiftLeft:
                    // TODO: Support for int64 - the semantics of wrapping around are different
                    return (int)left << (int)right;
                case BinaryOperation.ShiftRight:
                    // TODO: Support for int64
                    return (int)left >> (int)right;
                case BinaryOperation.And:
                    return left & right;
                case BinaryOperation.Or:
                    return left | right;
                case BinaryOperation.Xor:
                    return left ^ right;
                default:
                    throw new NotImplementedException("Unimplemented binary integer expression");
            }
        }

        private static bool EvaluateConstantBinary(BinaryOperation operation, bool left, bool right)
        {
            switch (operation)
            {
                case BinaryOperation.And:
                    return left & right;
                case BinaryOperation.Or:
                    return left | right;
                case BinaryOperation.Xor:
                    return left ^ right;
                case BinaryOperation.Equal:
                    return left == right;
                case BinaryOperation.NotEqual:
                    return left != right;
                default:
                    throw new NotImplementedException("Unimplemented binary Boolean expression");
            }
        }

        private static bool IsIntegerBinary(BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.Plus:
                case BinaryOperation.Minus:
                case BinaryOperation.Times:
                case BinaryOperation.Divide:
                case BinaryOperation.Modulo:
                case BinaryOperation.ShiftLeft:
                case BinaryOperation.ShiftRight:
                case BinaryOperation.And:
                case BinaryOperation.Or:
                case BinaryOperation.Xor:
                case BinaryOperation.LessThan:
                case BinaryOperation.LessThanOrEqual:
                case BinaryOperation.GreaterThan:
                case BinaryOperation.GreaterThanOrEqual:
                case BinaryOperation.Equal:
                case BinaryOperation.NotEqual:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBooleanBinary(BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.And:
                case BinaryOperation.Or:
                case BinaryOperation.Xor:
                case BinaryOperation.Equal:
                case BinaryOperation.NotEqual:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsComparison(BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.LessThan:
                case BinaryOperation.LessThanOrEqual:
                case BinaryOperation.GreaterThan:
                case BinaryOperation.GreaterThanOrEqual:
                case BinaryOperation.Equal:
                case BinaryOperation.NotEqual:
                    return true;
                default:
                    return false;
            }
        }

        private static bool EvaluateConstantComparison(BinaryOperation operation, long left, long right)
        {
            switch (operation)
            {
                case BinaryOperation.LessThan:
                    return left < right;
                case BinaryOperation.LessThanOrEqual:
                    return left <= right;
                case BinaryOperation.GreaterThan:
                    return left > right;
                case BinaryOperation.GreaterThanOrEqual:
                    return left >= right;
                case BinaryOperation.Equal:
                    return left == right;
                case BinaryOperation.NotEqual:
                    return left != right;
                default:
                    throw new NotImplementedException("Unimplemented comparison expression");
            }
        }

        private static string GetOperatorName(BinaryOperation operation)
        {
            switch (operation)
            {
                case BinaryOperation.Plus:
                    return "+";
                case BinaryOperation.Minus:
                    return "-";
                case BinaryOperation.Times:
                    return "*";
                case BinaryOperation.Divide:
                    return "/";
                case BinaryOperation.Modulo:
                    return "%";
                case BinaryOperation.ShiftLeft:
                    return "<<";
                case BinaryOperation.ShiftRight:
                    return ">>";
                case BinaryOperation.And:
                    return "&";
                case BinaryOperation.Or:
                    return "|";
                case BinaryOperation.Xor:
                    return "^";
                case BinaryOperation.ShortCircuitAnd:
                    return "&&";
                case BinaryOperation.ShortCircuitOr:
                    return "||";
                case BinaryOperation.LessThan:
                    return "<";
                case BinaryOperation.LessThanOrEqual:
                    return "<=";
                case BinaryOperation.GreaterThan:
                    return ">";
                case BinaryOperation.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperation.Equal:
                    return "==";
                case BinaryOperation.NotEqual:
                    return "!=";
                default:
                    throw new NotImplementedException("Unimplemented binary expression");
            }
        }
    }
}
