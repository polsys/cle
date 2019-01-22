using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// A Phi node used in the SSA form IR.
    /// </summary>
    public class Phi
    {
        /// <summary>
        /// Gets the destination local index.
        /// </summary>
        public int Destination { get; }

        /// <summary>
        /// Gets the operand local indices.
        /// </summary>
        [NotNull]
        public ImmutableList<int> Operands { get; }

        public Phi(int destination, [NotNull] ImmutableList<int> operands)
        {
            Destination = destination;
            Operands = operands;
        }
    }
}
