using Cle.Common;
using JetBrains.Annotations;

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
        [NotNull]
        public ExpressionSyntax ConditionSyntax { get; }

        /// <summary>
        /// Gets the block that will be executed while the condition is true.
        /// </summary>
        [NotNull]
        public BlockSyntax BodySyntax { get; }

        public WhileStatementSyntax(
            [NotNull] ExpressionSyntax condition,
            [NotNull] BlockSyntax body,
            TextPosition position)
            : base(position)
        {
            ConditionSyntax = condition;
            BodySyntax = body;
        }
    }
}
