using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Types of binary expressions recognized by the parser.
    /// </summary>
    public enum BinaryOperation
    {
        Invalid,
        Plus,
        Minus,
        Times,
        Divide,
        Modulo,
        And,
        ShortCircuitAnd,
        Or,
        ShortCircuitOr,
        Xor,
        ShiftLeft,
        ShiftRight,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Equal,
        NotEqual,
    }

    /// <summary>
    /// Syntax tree node for a binary expression with operation and two parameters.
    /// </summary>
    public sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        /// <summary>
        /// Gets the binary operation applying to the parameters.
        /// </summary>
        public BinaryOperation Operation { get; }

        /// <summary>
        /// Gets the left parameter to the binary operation.
        /// This parameter should be evaluated first.
        /// </summary>
        [NotNull]
        public ExpressionSyntax Left { get; }

        /// <summary>
        /// Gets the right parameter to the binary operation.
        /// This parameter should be evaluated after <see cref="Left"/>.
        /// </summary>
        [NotNull]
        public ExpressionSyntax Right { get; }

        public BinaryExpressionSyntax(BinaryOperation operation, [NotNull] ExpressionSyntax left,
            [NotNull] ExpressionSyntax right, TextPosition position)
            : base(position)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }
    }
}
