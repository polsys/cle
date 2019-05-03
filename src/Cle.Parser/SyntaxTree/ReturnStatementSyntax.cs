using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a return statement with either an expression or void value.
    /// </summary>
    public sealed class ReturnStatementSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the expression that is returned.
        /// Null for void returns.
        /// </summary>
        public ExpressionSyntax? ResultExpression { get; }

        public ReturnStatementSyntax(
            ExpressionSyntax? expression,
            TextPosition position)
            : base(position)
        {
            ResultExpression = expression;
        }
    }
}
