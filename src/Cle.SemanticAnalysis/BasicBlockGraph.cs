using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// An immutable graph of basic blocks.
    /// </summary>
    public class BasicBlockGraph
    {
        [NotNull]
        public readonly ImmutableList<BasicBlock> BasicBlocks;

        public BasicBlockGraph(ImmutableList<BasicBlock> basicBlocks)
        {
            BasicBlocks = basicBlocks;
        }

        // TODO: Override Equals and GetHashCode
    }
}
