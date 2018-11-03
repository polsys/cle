using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// An immutable basic block of linearly executable instructions.
    /// </summary>
    public readonly struct BasicBlock
    {
        /// <summary>
        /// Gets the immutable linear list of instructions.
        /// </summary>
        [NotNull]
        public ImmutableList<Instruction> Instructions { get; }

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

        public BasicBlock(ImmutableList<Instruction> instructions, int defaultSuccessor, int alternativeSuccessor)
        {
            Instructions = instructions;
            DefaultSuccessor = defaultSuccessor;
            AlternativeSuccessor = alternativeSuccessor;
        }
        
        // TODO: Consider implementing equality comparisons (complication: ImmutableList<T> does not override Equals)
    }
}
