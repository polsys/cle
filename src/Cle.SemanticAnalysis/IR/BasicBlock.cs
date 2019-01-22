using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// An immutable basic block of linearly executable instructions.
    /// </summary>
    public class BasicBlock
    {
        /// <summary>
        /// Gets the immutable linear list of instructions.
        /// </summary>
        [NotNull]
        public ImmutableList<Instruction> Instructions { get; }

        /// <summary>
        /// Gets the Phi nodes in this basic block.
        /// </summary>
        [NotNull]
        public ImmutableList<Phi> Phis { get; }

        /// <summary>
        /// Gets the index of the succeeding basic block.
        /// This may be -1 if this basic block ends in a return.
        /// If this basic block ends in a branch, <see cref="AlternativeSuccessor"/> may be used instead.
        /// </summary>
        public int DefaultSuccessor { get; }

        /// <summary>
        /// Gets the index of the succeeding basic block in case a branch is executed.
        /// If this basic block does not end in a branch, this should be equal to -1.
        /// </summary>
        public int AlternativeSuccessor { get; }

        /// <summary>
        /// Gets the predecessors of this basic block in an arbitrary order.
        /// </summary>
        [NotNull]
        public ImmutableList<int> Predecessors { get; }

        public BasicBlock([NotNull] ImmutableList<Instruction> instructions,
            [NotNull] ImmutableList<Phi> phis,
            int defaultSuccessor, int alternativeSuccessor,
            [NotNull] ImmutableList<int> predecessors)
        {
            Instructions = instructions;
            Phis = phis;
            DefaultSuccessor = defaultSuccessor;
            AlternativeSuccessor = alternativeSuccessor;
            Predecessors = predecessors;
        }
        
        // TODO: Consider implementing equality comparisons (complication: ImmutableList<T> does not override Equals)
    }
}
