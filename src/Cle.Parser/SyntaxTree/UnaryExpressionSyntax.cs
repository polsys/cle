using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Types of unary expressions recognized by the parser.
    /// </summary>
    public enum UnaryOperation
    {
        Invalid,
        Minus,
        Negation,
        Complement
    }

    /// <summary>
    /// Syntax tree node for a unary expression with operation and parameter.
    /// </summary>
    public sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        /// <summary>
        /// Gets the unary operation applying to <see cref="InnerExpression"/>.
        /// </summary>
        public UnaryOperation Operation { get; }

        /// <summary>
        /// Gets the parameter to the unary operation.
        /// </summary>
        [NotNull]
        public ExpressionSyntax InnerExpression { get; }

        public UnaryExpressionSyntax(UnaryOperation operation, [NotNull] ExpressionSyntax innerExpression,
            TextPosition position)
            : base(position)
        {
            Operation = operation;
            InnerExpression = innerExpression;
        }
    }
}
