using System;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Provides static methods for compiling expressions.
    /// </summary>
    internal static class ExpressionCompiler
    {
        /// <summary>
        /// Compiles the given expression syntax tree and verifies its type.
        /// Returns the local index if successful, returns -1 and logs diagnostics if the compilation fails.
        /// </summary>
        /// <param name="syntax">The root of the expression syntax tree.</param>
        /// <param name="expectedType">The expected type of the evaluated expression.</param>
        /// <param name="method">The method to store and get local values from.</param>
        /// <param name="builder">Intermediate representation builder.</param>
        /// <param name="variableMap">The map of variable names to local indices.</param>
        /// <param name="diagnostics">Receiver for possible diagnostics.</param>
        public static int TryCompileExpression(
            [NotNull] ExpressionSyntax syntax,
            [NotNull] TypeDefinition expectedType,
            [NotNull] CompiledMethod method,
            [NotNull] BasicBlockBuilder builder,
            [NotNull] ScopedVariableMap variableMap,
            [NotNull] IDiagnosticSink diagnostics)
        {
            // Compile the expression
            if (!InternalTryCompileExpression(syntax, method, builder, variableMap, diagnostics, out var value))
            {
                return -1;
            }

            // Verify the type
            // TODO: Once multiple integer types exist, there must be some notion of conversions
            if (!value.Type.Equals(expectedType))
            {
                diagnostics.Add(DiagnosticCode.TypeMismatch, syntax.Position, 
                    value.Type.TypeName, expectedType.TypeName);
                return -1;
            }

            // Also, if the expression is constant, verify that it is representable
            if (!value.LocalIndex.HasValue && value.Type.Equals(SimpleType.Int32))
            {
                if (value.ConstantValue.AsSignedInteger < int.MinValue ||
                    value.ConstantValue.AsSignedInteger > int.MaxValue)
                {
                    diagnostics.Add(DiagnosticCode.IntegerConstantOutOfBounds, syntax.Position,
                        value.ConstantValue.ToString(), SimpleType.Int32.TypeName);
                    return -1;
                }
            }

            // Store the value in a local
            return EnsureValueIsStored(value, method);
        }

        private static bool InternalTryCompileExpression(
            [NotNull] ExpressionSyntax syntax,
            [NotNull] CompiledMethod method,
            [NotNull] BasicBlockBuilder builder,
            [NotNull] ScopedVariableMap variableMap,
            [NotNull] IDiagnosticSink diagnostics,
            out Temporary value)
        {
            value = default;

            if (syntax is IntegerLiteralSyntax integer)
            {
                // Decide the type for the literal
                // TODO: There is for now only support for int32 and uint32, and only because the smallest int32 should be
                // TODO: representable. This logic should be updated once multiple integer types officially exist.
                if (integer.Value > uint.MaxValue)
                {
                    diagnostics.Add(DiagnosticCode.IntegerConstantOutOfBounds, syntax.Position,
                        integer.Value.ToString(), SimpleType.Int32.TypeName);
                    return false;
                }
                else if (integer.Value > int.MaxValue)
                {
                    // TODO: This does not work for anything else than int32 minimum
                    value = Temporary.FromConstant(SimpleType.UInt32, ConstantValue.SignedInteger((long)integer.Value));
                    return true;
                }
                else
                {
                    value = Temporary.FromConstant(SimpleType.Int32, ConstantValue.SignedInteger((long)integer.Value));
                    return true;
                }
            }
            else if (syntax is BooleanLiteralSyntax boolean)
            {
                value = Temporary.FromConstant(SimpleType.Bool, ConstantValue.Bool(boolean.Value));
                return true;
            }
            else if (syntax is NamedValueSyntax named)
            {
                if (!variableMap.TryGetVariable(named.Name, out var valueNumber))
                {
                    diagnostics.Add(DiagnosticCode.VariableNotFound, named.Position, named.Name);
                    return false;
                }

                value = Temporary.FromLocal(method.Values[valueNumber].Type, valueNumber);
                return true;
            }
            else if (syntax is UnaryExpressionSyntax unary)
            {
                if (!InternalTryCompileExpression(unary.InnerExpression, method, builder, variableMap, diagnostics,
                    out var inner))
                {
                    return false;
                }

                return TryCompileUnary(unary, inner, method, builder, diagnostics, out value);
            }
            else if (syntax is BinaryExpressionSyntax binary)
            {
                if (!InternalTryCompileExpression(binary.Left, method, builder, variableMap, diagnostics, out var left) ||
                    !InternalTryCompileExpression(binary.Right, method, builder, variableMap, diagnostics, out var right))
                {
                    return false;
                }

                return TryCompileBinary(binary, left, right, method, builder, diagnostics, out value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static bool TryCompileUnary(UnaryExpressionSyntax expression, in Temporary innerValue,
            CompiledMethod method, BasicBlockBuilder builder, IDiagnosticSink diagnostics, out Temporary value)
        {
            // TODO: Support for other types
            if (!innerValue.Type.Equals(SimpleType.Int32) && !innerValue.Type.Equals(SimpleType.UInt32))
            {
                diagnostics.Add(DiagnosticCode.OperatorNotDefined, expression.Position, 
                    GetOperatorName(expression.Operation), innerValue.Type.TypeName);
                value = default;
                return false;
            }

            if (innerValue.LocalIndex.HasValue)
            {
                // If the value is stored in a local, emit a negation instruction
                var destination = method.AddLocal(SimpleType.Int32, ConstantValue.Void());
                value = Temporary.FromLocal(SimpleType.Int32, destination);

                builder.AppendInstruction(GetUnaryOpcode(expression.Operation), innerValue.LocalIndex.Value, 0, destination);
                return true;
            }
            else
            {
                // Else, the expression can be evaluated at compile time
                var evaluated = EvaluateConstantUnary(expression.Operation, innerValue.ConstantValue.AsSignedInteger);
                value = Temporary.FromConstant(SimpleType.Int32, ConstantValue.SignedInteger(evaluated));
                return true;
            }
        }

        private static bool TryCompileBinary(BinaryExpressionSyntax binary, in Temporary left, in Temporary right,
            CompiledMethod method, BasicBlockBuilder builder, IDiagnosticSink diagnostics, out Temporary value)
        {
            value = default;

            // TODO: Support for other numeric types
            // TODO: Support for non-integer binary expressions
            if (!left.Type.Equals(SimpleType.Int32))
            {
                diagnostics.Add(DiagnosticCode.OperatorNotDefined, binary.Position, 
                    GetOperatorName(binary.Operation), left.Type.TypeName);
                return false;
            }
            else if (!left.Type.Equals(right.Type))
            {
                diagnostics.Add(DiagnosticCode.TypeMismatch, binary.Position, right.Type.TypeName, SimpleType.Int32.TypeName);
                return false;
            }

            if (left.LocalIndex.HasValue || right.LocalIndex.HasValue)
            {
                // Evaluate the value at run time
                if (IsDivisionByZero(binary, right, diagnostics))
                {
                    return false;
                }

                var leftIndex = EnsureValueIsStored(left, method);
                var rightIndex = EnsureValueIsStored(right, method);
                var opcode = GetBinaryOpcode(binary.Operation);

                var destination = method.AddLocal(SimpleType.Int32, ConstantValue.Void());
                value = Temporary.FromLocal(SimpleType.Int32, destination);
                builder.AppendInstruction(opcode, leftIndex, rightIndex, destination);
                return true;
            }
            else
            {
                // Evaluate the value at compile time
                if (IsDivisionByZero(binary, right, diagnostics))
                {
                    return false;
                }
                if (binary.Operation == BinaryOperation.Modulo &&
                    left.ConstantValue.AsSignedInteger == int.MinValue &&
                    right.ConstantValue.AsSignedInteger == -1)
                {
                    // int.MinValue % -1 causes overflow
                    diagnostics.Add(DiagnosticCode.IntegerConstantOutOfBounds, binary.Position);
                    return false;
                }

                var evaluated = EvaluateConstantBinary(binary.Operation,
                    left.ConstantValue.AsSignedInteger, right.ConstantValue.AsSignedInteger);
                value = Temporary.FromConstant(SimpleType.Int32, ConstantValue.SignedInteger(evaluated));
                return true;
            }
        }

        private static bool IsDivisionByZero(BinaryExpressionSyntax binary,
            in Temporary right, IDiagnosticSink diagnostics)
        {
            if ((binary.Operation == BinaryOperation.Divide || binary.Operation == BinaryOperation.Modulo) &&
                right.ConstantValue.Equals(ConstantValue.SignedInteger(0)))
            {
                diagnostics.Add(DiagnosticCode.DivisionByConstantZero, binary.Position);
                return true;
            }

            return false;
        }

        private static Opcode GetUnaryOpcode(UnaryOperation operation)
        {
            switch (operation)
            {
                case UnaryOperation.Minus:
                    return Opcode.ArithmeticNegate;
                case UnaryOperation.Complement:
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
                    throw new NotImplementedException("Unimplemented binary expression");
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
                    throw new NotImplementedException("Unimplemented binary expression");
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
                default:
                    throw new NotImplementedException("Unimplemented binary expression");
            }
        }

        private static int EnsureValueIsStored(in Temporary value, CompiledMethod method)
        {
            return value.LocalIndex ?? method.AddLocal(value.Type, value.ConstantValue);
        }

        /// <summary>
        /// A temporary value, either a constant or a local, produced in expression compilation.
        /// </summary>
        private readonly struct Temporary
        {
            public readonly int? LocalIndex;
            public readonly TypeDefinition Type;
            public readonly ConstantValue ConstantValue;

            private Temporary(int? localIndex, TypeDefinition type, ConstantValue constantValue)
            {
                LocalIndex = localIndex;
                Type = type;
                ConstantValue = constantValue;
            }

            public static Temporary FromConstant(SimpleType type, ConstantValue value)
            {
                return new Temporary(null, type, value);
            }

            public static Temporary FromLocal(TypeDefinition type, int localIndex)
            {
                return new Temporary(localIndex, type, ConstantValue.Void());
            }
        }
    }
}
