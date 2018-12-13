using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// An immutable graph of basic blocks.
    /// </summary>
    public class BasicBlockGraph
    {
        [NotNull]
        [ItemCanBeNull]
        public readonly ImmutableList<BasicBlock> BasicBlocks;

        public BasicBlockGraph([NotNull, ItemCanBeNull] ImmutableList<BasicBlock> basicBlocks)
        {
            BasicBlocks = basicBlocks;
        }

        // TODO: Override Equals and GetHashCode
    }
}
