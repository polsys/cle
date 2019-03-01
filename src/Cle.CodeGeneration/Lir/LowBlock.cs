using System.Collections.Generic;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

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
        [NotNull]
        public List<LowInstruction> Instructions = new List<LowInstruction>();

        /// <summary>
        /// Gets a read-only list of Phi nodes executed at the start of this block.
        /// Can be null.
        /// </summary>
        [CanBeNull, ItemNotNull]
        public IReadOnlyList<Phi> Phis;
    }
}
