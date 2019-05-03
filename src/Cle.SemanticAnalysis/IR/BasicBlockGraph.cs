using System.Collections.Immutable;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// An immutable graph of basic blocks.
    /// </summary>
    public class BasicBlockGraph
    {
        public readonly ImmutableList<BasicBlock?> BasicBlocks;

        public BasicBlockGraph(ImmutableList<BasicBlock?> basicBlocks)
        {
            BasicBlocks = basicBlocks;
        }

        // TODO: Override Equals and GetHashCode
    }
}
