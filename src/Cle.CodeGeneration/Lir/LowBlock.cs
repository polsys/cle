using System;
using System.Collections.Generic;
using Cle.SemanticAnalysis.IR;

namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// A basic block of a LIR method.
    /// </summary>
    internal class LowBlock
    {
        /// <summary>
        /// Gets a linear list of lowered instructions in this block.
        /// </summary>
        public readonly List<LowInstruction> Instructions;

        /// <summary>
        /// Gets a read-only list of Phi nodes executed at the start of this block.
        /// Can be null.
        /// </summary>
        public IReadOnlyList<Phi>? Phis;

        /// <summary>
        /// Gets a list of predecessors of this basic block.
        /// </summary>
        public IReadOnlyList<int> Predecessors;

        /// <summary>
        /// Gets a list of successors of this basic block.
        /// </summary>
        public IReadOnlyList<int> Successors;

        public LowBlock() : this(new List<LowInstruction>())
        {
        }

        public LowBlock(List<LowInstruction> instructions)
        {
            Instructions = instructions;
            Predecessors = Array.Empty<int>();
            Successors = Array.Empty<int>();
        }
    }
}
