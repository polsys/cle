using System.Collections.Immutable;
using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a block of statements.
    /// </summary>
    public sealed class BlockSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the ordered list of statements within this block.
        /// </summary>
        [NotNull, ItemNotNull]
        public ImmutableList<StatementSyntax> Statements { get; }

        public BlockSyntax(
            [NotNull, ItemNotNull] ImmutableList<StatementSyntax> statements,
            TextPosition position)
            : base(position)
        {
            Statements = statements;
        }
    }
}
