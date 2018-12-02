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
        /// Gets what will be executed when the condition if false.
        /// The type of this depends on the kind of 'else':
        ///  - null, if there is no 'else' block,
        ///  - BlockSyntax, if there is an 'else' block,
        ///  - IfStatementSyntax, if there is an 'else if' statement.
        /// </summary>
        [CanBeNull]
        public StatementSyntax ElseSyntax { get; }

        public IfStatementSyntax(
            [NotNull] ExpressionSyntax condition,
            [NotNull] BlockSyntax thenBlock,
            [CanBeNull] StatementSyntax elseSyntax,
            TextPosition position)
            : base(position)
        {
            ConditionSyntax = condition;
            ThenBlockSyntax = thenBlock;
            ElseSyntax = elseSyntax;
        }
    }
}
