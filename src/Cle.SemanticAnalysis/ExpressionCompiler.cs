using System;
using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
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
        /// <param name="diagnostics">Receiver for possible diagnostics.</param>
        // TODO: Access to local variables and constants
        public static int TryCompileExpression(
            [NotNull] ExpressionSyntax syntax,
            [NotNull] TypeDefinition expectedType,
            [NotNull] CompiledMethod method,
            [NotNull] BasicBlockBuilder builder,
            [NotNull] IDiagnosticSink diagnostics)
        {
            // Compile the expression
            // TODO: Handling more complicated expressions, which may not be constant
            var valueNumber = -1;
            if (syntax is IntegerLiteralSyntax integer)
            {
                // Ensure that the literal can be represented as int32
                // TODO: This logic needs to be updated once multiple integer types exist
                // TODO: ...and once unary minus is handled
                // TODO: ...but probably more complicated expressions should just overflow their types
                if (integer.Value > int.MaxValue)
                {
                    diagnostics.Add(DiagnosticCode.IntegerLiteralOutOfBounds, syntax.Position, 
                        integer.Value.ToString(), SimpleType.Int32.TypeName);
                    return -1;
                }
                
                valueNumber = method.AddTemporary(SimpleType.Int32, ConstantValue.SignedInteger((long)integer.Value));
            }
            else if (syntax is BooleanLiteralSyntax boolean)
            {
                valueNumber = method.AddTemporary(SimpleType.Bool, ConstantValue.Bool(boolean.Value));
            }
            else
            {
                throw new NotImplementedException();
            }

            // Verify the type
            // TODO: Once multiple integer types exist, there must be some notion of conversions
            var actualType = method.Values[valueNumber].Type;
            if (!actualType.Equals(expectedType))
            {
                diagnostics.Add(DiagnosticCode.TypeMismatch, syntax.Position, actualType.TypeName, expectedType.TypeName);
                return -1;
            }

            return valueNumber;
        }
    }
}
