using System.Collections.Generic;

namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// A basic block of a LIR method.
    /// </summary>
    internal class LowBlock
    {
        public List<LowInstruction> Instructions = new List<LowInstruction>();

        // TODO: Phis to be resolved at the end of this block
    }
}
