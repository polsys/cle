using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for an 'while' loop statement.
    /// </summary>
    public sealed class WhileStatementSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the condition expression.
        /// </summary>
        public ExpressionSyntax ConditionSyntax { get; }

        /// <summary>
        /// Gets the block that will be executed while the condition is true.
        /// </summary>
        public BlockSyntax BodySyntax { get; }

        public WhileStatementSyntax(
            ExpressionSyntax condition,
            BlockSyntax body,
            TextPosition position)
            : base(position)
        {
            ConditionSyntax = condition;
            BodySyntax = body;
        }
    }
}
