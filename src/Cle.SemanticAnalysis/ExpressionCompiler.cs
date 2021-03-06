﻿using System;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Provides static methods for compiling expressions.
    /// </summary>
    internal static partial class ExpressionCompiler
    {
        // TODO: Benchmark whether it would be beneficial to pass the unchanging parameters to the private methods
        //       in a readonly struct. We pass a lot of parameters and the call trees run quite deep.

        /// <summary>
        /// Compiles the given expression syntax tree and verifies its type.
        /// Returns the local index if successful, returns -1 and logs diagnostics if the compilation fails.
        /// </summary>
        /// <param name="syntax">The root of the expression syntax tree.</param>
        /// <param name="expectedType">The expected type of the evaluated expression. If null, the type is not checked.</param>
        /// <param name="method">The method to store and get local values from.</param>
        /// <param name="builder">Intermediate representation builder.</param>
        /// <param name="nameResolver">The resolver for variable and method names.</param>
        /// <param name="diagnostics">Receiver for possible diagnostics.</param>
        public static int TryCompileExpression(
            ExpressionSyntax syntax,
            TypeDefinition? expectedType,
            CompiledMethod method,
            BasicBlockBuilder builder,
            INameResolver nameResolver,
            IDiagnosticSink diagnostics)
        {
            // Compile the expression
            if (!InternalTryCompileExpression(syntax, method, builder, nameResolver, diagnostics, out var value))
            {
                return -1;
            }

            // Verify the type
            // TODO: Once multiple integer types exist, there must be some notion of conversions
            if (expectedType != null && !value.Type.Equals(expectedType))
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
            return EnsureValueIsStored(value, method, builder);
        }

        private static bool InternalTryCompileExpression(
            ExpressionSyntax syntax,
            CompiledMethod method,
            BasicBlockBuilder builder,
            INameResolver nameResolver,
            IDiagnosticSink diagnostics,
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
            else if (syntax is IdentifierSyntax named)
            {
                if (!nameResolver.TryResolveVariable(named.Name, out var valueNumber))
                {
                    diagnostics.Add(DiagnosticCode.VariableNotFound, named.Position, named.Name);
                    return false;
                }

                value = Temporary.FromLocal(method.Values[valueNumber].Type, (ushort)valueNumber);
                return true;
            }
            else if (syntax is UnaryExpressionSyntax unary)
            {
                if (!InternalTryCompileExpression(unary.InnerExpression, method, builder, nameResolver, diagnostics,
                    out var inner))
                {
                    return false;
                }

                return TryCompileUnary(unary, inner, method, builder, diagnostics, out value);
            }
            else if (syntax is BinaryExpressionSyntax binary)
            {
                if (!InternalTryCompileExpression(binary.Left, method, builder, nameResolver, diagnostics, out var left) ||
                    !InternalTryCompileExpression(binary.Right, method, builder, nameResolver, diagnostics, out var right))
                {
                    return false;
                }

                return TryCompileBinary(binary, left, right, method, builder, diagnostics, out value);
            }
            else if (syntax is FunctionCallSyntax call)
            {
                return TryCompileCall(call, method, builder, diagnostics, nameResolver, out value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static bool TryCompileCall(FunctionCallSyntax call, CompiledMethod method, BasicBlockBuilder builder,
            IDiagnosticSink diagnostics, INameResolver nameResolver, out Temporary value)
        {
            value = default;

            // Get the callee
            var matchingMethods = nameResolver.ResolveMethod(call.Function.Name);
            if (matchingMethods.Count == 0)
            {
                diagnostics.Add(DiagnosticCode.MethodNotFound, call.Function.Position, call.Function.Name);
                return false;
            }
            else if (matchingMethods.Count > 1)
            {
                // TODO: Test this case
                throw new NotImplementedException("Multiple matching methods");
            }
            var declaration = matchingMethods[0];

            // Assert that there is the right number of parameters
            if (call.Parameters.Count != declaration.ParameterTypes.Count)
            {
                diagnostics.Add(DiagnosticCode.ParameterCountMismatch, call.Position,
                    actual: call.Parameters.Count.ToString(), expected: declaration.ParameterTypes.Count.ToString());
                return false;
            }

            // Evaluate the parameters, verifying their types
            var parameterIndices = new int[declaration.ParameterTypes.Count];
            for (var i = 0; i < parameterIndices.Length; i++)
            {
                var paramIndex = TryCompileExpression(call.Parameters[i], declaration.ParameterTypes[i],
                    method, builder, nameResolver, diagnostics);

                // If the compilation of the expression failed for some reason, the diagnostic is already logged
                if (paramIndex == -1)
                {
                    return false;
                }
                parameterIndices[i] = paramIndex;
            }

            // Emit a call operation
            var callType = declaration is ImportedMethodDeclaration ? MethodCallType.Imported : MethodCallType.Native;

            var callInfoIndex = method.AddCallInfo(declaration.BodyIndex, parameterIndices, declaration.FullName, callType);
            var resultIndex = method.AddLocal(declaration.ReturnType, LocalFlags.None);
            builder.AppendInstruction(Opcode.Call, callInfoIndex, 0, resultIndex);

            value = Temporary.FromLocal(declaration.ReturnType, resultIndex);
            return true;
        }

        private static bool TryCompileUnary(UnaryExpressionSyntax expression, in Temporary innerValue,
            CompiledMethod method, BasicBlockBuilder builder, IDiagnosticSink diagnostics, out Temporary value)
        {
            if (innerValue.Type is SimpleType simple && simple.IsInteger && IsIntegerUnary(expression.Operation))
            {
                CompileIntegerUnary(expression, in innerValue, method, builder, out value);
                return true;
            }
            else if (innerValue.Type.Equals(SimpleType.Bool) && expression.Operation == UnaryOperation.Negation)
            {
                CompileBooleanUnary(expression, in innerValue, method, builder, out value);
                return true;
            }
            else
            {
                // TODO: Support for floating-point types
                // TODO: Support for user-defined operators
                diagnostics.Add(DiagnosticCode.OperatorNotDefined, expression.Position, 
                    GetOperatorName(expression.Operation), innerValue.Type.TypeName);
                value = default;
                return false;
            }
        }

        private static void CompileIntegerUnary(UnaryExpressionSyntax expression, in Temporary innerValue,
            CompiledMethod method, BasicBlockBuilder builder, out Temporary value)
        {
            if (innerValue.LocalIndex.HasValue)
            {
                // If the value is already stored in a local, emit a suitable run-time instruction
                var destination = method.AddLocal(SimpleType.Int32, LocalFlags.None);
                value = Temporary.FromLocal(SimpleType.Int32, destination);

                builder.AppendInstruction(GetUnaryOpcode(expression.Operation), innerValue.LocalIndex.Value, 0, destination);
            }
            else
            {
                // Else, the expression can be evaluated at compile time
                var evaluated = EvaluateConstantUnary(expression.Operation, innerValue.ConstantValue.AsSignedInteger);
                value = Temporary.FromConstant(SimpleType.Int32, ConstantValue.SignedInteger(evaluated));
            }
        }

        private static void CompileBooleanUnary(UnaryExpressionSyntax expression, in Temporary innerValue,
            CompiledMethod method, BasicBlockBuilder builder, out Temporary value)
        {
            if (innerValue.LocalIndex.HasValue)
            {
                // If the value is already stored in a local, emit a suitable run-time instruction
                var destination = method.AddLocal(SimpleType.Bool, LocalFlags.None);
                value = Temporary.FromLocal(SimpleType.Bool, destination);

                builder.AppendInstruction(GetUnaryOpcode(expression.Operation), innerValue.LocalIndex.Value, 0, destination);
            }
            else
            {
                // Else, the expression can be evaluated at compile time
                var evaluated = EvaluateConstantUnary(expression.Operation, innerValue.ConstantValue.AsBool);
                value = Temporary.FromConstant(SimpleType.Bool, ConstantValue.Bool(evaluated));
            }
        }

        private static bool TryCompileBinary(BinaryExpressionSyntax binary, in Temporary left, in Temporary right,
            CompiledMethod method, BasicBlockBuilder builder, IDiagnosticSink diagnostics, out Temporary value)
        {
            value = default;

            // Currently, all binary operations are symmetric in their parameter types
            // (This won't be the case with user-defined operators)
            if (!left.Type.Equals(right.Type))
            {
                diagnostics.Add(DiagnosticCode.TypeMismatch, binary.Position, right.Type.TypeName, left.Type.TypeName);
                return false;
            }

            // Check the type of the operation
            if (left.Type is SimpleType simpleType && simpleType.IsInteger && IsIntegerBinary(binary.Operation))
            {
                // This is an integer expression.
                // First, check that the operation is valid.
                if (IsDivisionByZero(binary, right, diagnostics))
                {
                    return false;
                }
                
                if (left.LocalIndex.HasValue || right.LocalIndex.HasValue)
                {
                    // If at least one of the parameters is non-constant, we have to evaluate the expression
                    // at run time. We know that this can be done due to the IsIntegerBinary check.
                    var resultType = IsComparison(binary.Operation) ? SimpleType.Bool : simpleType;
                    CompileRuntimeBinary(binary.Operation, in left, in right, resultType, method, builder, out value);
                }
                else
                {
                    // Both parameters are compile time constants: evaluate the expression now.
                    // One more precondition check: int.MinValue % -1 causes overflow.
                    if (binary.Operation == BinaryOperation.Modulo &&
                        left.ConstantValue.AsSignedInteger == int.MinValue &&
                        right.ConstantValue.AsSignedInteger == -1)
                    {
                        diagnostics.Add(DiagnosticCode.IntegerConstantOutOfBounds, binary.Position);
                        return false;
                    }
                    
                    // Comparisons produce booleans, the remaining operators integers
                    if (IsComparison(binary.Operation))
                    {
                        var evaluated = EvaluateConstantComparison(binary.Operation,
                            left.ConstantValue.AsSignedInteger, right.ConstantValue.AsSignedInteger);
                        value = Temporary.FromConstant(SimpleType.Bool, ConstantValue.Bool(evaluated));
                    }
                    else
                    {
                        var evaluated = EvaluateConstantBinary(binary.Operation,
                            left.ConstantValue.AsSignedInteger, right.ConstantValue.AsSignedInteger);
                        value = Temporary.FromConstant(SimpleType.Int32, ConstantValue.SignedInteger(evaluated));
                    }
                }
            }
            else if (left.Type.Equals(SimpleType.Bool) && IsBooleanBinary(binary.Operation))
            {
                // Boolean expression
                if (left.LocalIndex.HasValue || right.LocalIndex.HasValue)
                {
                    // Run time evaluation
                    CompileRuntimeBinary(binary.Operation, in left, in right, SimpleType.Bool, method, builder, out value);
                }
                else
                {
                    // Compile time evaluation
                    var evaluated = EvaluateConstantBinary(binary.Operation,
                        left.ConstantValue.AsBool, right.ConstantValue.AsBool);
                    value = Temporary.FromConstant(SimpleType.Bool, ConstantValue.Bool(evaluated));
                }
            }
            else
            {
                // TODO: Support for floating-point types
                // TODO: Support for user-defined operators
                diagnostics.Add(DiagnosticCode.OperatorNotDefined, binary.Position,
                    GetOperatorName(binary.Operation), left.Type.TypeName);
                return false;
            }

            return true;
        }

        private static void CompileRuntimeBinary(BinaryOperation operation, in Temporary left, in Temporary right,
            SimpleType resultType, CompiledMethod method, BasicBlockBuilder builder, out Temporary value)
        {
            // This method assumes that the operation is valid to do with the given arguments

            var leftIndex = EnsureValueIsStored(left, method, builder);
            var rightIndex = EnsureValueIsStored(right, method, builder);

            var destination = method.AddLocal(resultType, LocalFlags.None);
            value = Temporary.FromLocal(resultType, destination);

            // There are no >, >= or != instructions in the IL, so we have to emulate them
            switch (operation)
            {
                case BinaryOperation.GreaterThan:
                    // Swap the operands
                    builder.AppendInstruction(Opcode.Less, rightIndex, leftIndex, destination);
                    break;
                case BinaryOperation.GreaterThanOrEqual:
                    // As above
                    builder.AppendInstruction(Opcode.LessOrEqual, rightIndex, leftIndex, destination);
                    break;
                case BinaryOperation.NotEqual:
                    // This is a bit more complicated: we have to emit !(a == b).
                    // This means creating another temporary, which will be the final destination
                    var finalDestination = method.AddLocal(resultType, LocalFlags.None);
                    value = Temporary.FromLocal(resultType, finalDestination);

                    builder.AppendInstruction(Opcode.Equal, leftIndex, rightIndex, destination);
                    builder.AppendInstruction(Opcode.BitwiseNot, destination, 0, finalDestination);
                    break;
                default:
                    builder.AppendInstruction(GetBinaryOpcode(operation), leftIndex, rightIndex, destination);
                    break;
            }
        }

        private static bool IsDivisionByZero(BinaryExpressionSyntax binary, in Temporary right, IDiagnosticSink diagnostics)
        {
            if ((binary.Operation == BinaryOperation.Divide || binary.Operation == BinaryOperation.Modulo)
                && right.ConstantValue.Equals(ConstantValue.SignedInteger(0)))
            {
                diagnostics.Add(DiagnosticCode.DivisionByConstantZero, binary.Position);
                return true;
            }

            return false;
        }

        private static ushort EnsureValueIsStored(in Temporary value, CompiledMethod method, BasicBlockBuilder builder)
        {
            if (value.LocalIndex.HasValue)
            {
                return value.LocalIndex.Value;
            }
            else
            {
                var local = method.AddLocal(value.Type, LocalFlags.None);
                builder.AppendInstruction(Opcode.Load, value.ConstantValue.AsUnsignedInteger, 0, local);
                return local;
            }
        }

        /// <summary>
        /// A temporary value, either a constant or a local, produced in expression compilation.
        /// </summary>
        private readonly struct Temporary
        {
            public readonly ushort? LocalIndex;
            public readonly TypeDefinition Type;
            public readonly ConstantValue ConstantValue;

            private Temporary(ushort? localIndex, TypeDefinition type, ConstantValue constantValue)
            {
                LocalIndex = localIndex;
                Type = type;
                ConstantValue = constantValue;
            }

            public static Temporary FromConstant(SimpleType type, ConstantValue value)
            {
                return new Temporary(null, type, value);
            }

            public static Temporary FromLocal(TypeDefinition type, ushort localIndex)
            {
                return new Temporary(localIndex, type, ConstantValue.Void());
            }
        }
    }
}
