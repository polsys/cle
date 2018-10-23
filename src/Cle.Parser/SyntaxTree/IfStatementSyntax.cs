using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for an 'if' statement with possible 'else' branch.
    /// </summary>
    public sealed class IfStatementSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the condition expression.
        /// </summary>
        [NotNull]
        public ExpressionSyntax ConditionSyntax { get; }

        /// <summary>
        /// Gets the block that will be executed when the condition is true.
        /// </summary>
        [NotNull]
        public BlockSyntax ThenBlockSyntax { get; }

        /// <summary>
        /// Gets the block that will be executed when the condition is false.
        /// If there is no 'else' block, this will be null.
        /// 'Else if' is represented as an 'if' statement within this block.
        /// </summary>
        [CanBeNull]
        public BlockSyntax ElseBlockSyntax { get; }

        public IfStatementSyntax(
            [NotNull] ExpressionSyntax condition,
            [NotNull] BlockSyntax thenBlock,
            [CanBeNull] BlockSyntax elseBlock,
            TextPosition position)
            : base(position)
        {
            ConditionSyntax = condition;
            ThenBlockSyntax = thenBlock;
            ElseBlockSyntax = elseBlock;
        }
    }
}
