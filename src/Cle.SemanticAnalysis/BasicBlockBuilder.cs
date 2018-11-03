using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// A builder for <see cref="BasicBlock"/> instances.
    /// Instances of this type are managed by <see cref="BasicBlockGraphBuilder"/>.
    /// </summary>
    internal class BasicBlockBuilder
    {
        [NotNull]
        internal ImmutableList<Instruction>.Builder Instructions { get; } = ImmutableList<Instruction>.Empty.ToBuilder();

        /// <summary>
        /// Gets the index of the next basic block (unless a branch is taken).
        /// Will be -1 if undefined or this block returns.
        /// </summary>
        public int DefaultSuccessor { get; private set; } = -1;

        /// <summary>
        /// Gets the index of the next basic block in case a branch is taken.
        /// If this basic block does not end in a branch, this is set to -1.
        /// </summary>
        public int AlternativeSuccessor { get; private set; } = -1;

        /// <summary>
        /// Returns true, iff
        ///   the last instruction is a return, or
        ///   the last instruction is a branch and two successors are specified, or
        ///   the succeeding basic block is specified.
        /// </summary>
        public bool HasDefinedExitBehavior
        {
            get
            {
                if (Instructions.Count > 0)
                {
                    if (Instructions[Instructions.Count - 1].Operation == Opcode.Return)
                    {
                        // Last instruction is a return
                        return true;
                    }
                    else if (Instructions[Instructions.Count - 1].Operation == Opcode.BranchIf)
                    {
                        // Last instruction is a branch and the successors are defined
                        return DefaultSuccessor >= 0 && AlternativeSuccessor >= 0;
                    }
                }

                // Or, a default successor is specified
                return DefaultSuccessor >= 0;
            }
        }

        [NotNull]
        private readonly BasicBlockGraphBuilder _parent;

        internal BasicBlockBuilder([NotNull] BasicBlockGraphBuilder parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Appends an instruction with the given parameters.
        /// If this block already has a defined exit, throws.
        /// </summary>
        public void AppendInstruction(Opcode opcode, int left, int right, int destination)
        {
            if (HasDefinedExitBehavior)
                throw new InvalidOperationException("This basic block already has an exit.");

            Instructions.Add(new Instruction(opcode, left, right, destination));
        }

        /// <summary>
        /// Creates a new basic block builder and sets <see cref="DefaultSuccessor"/> to point to the new basic block.
        /// This method may not be called if the successor is already set.
        /// </summary>
        public BasicBlockBuilder CreateSuccessorBlock()
        {
            if (HasDefinedExitBehavior)
                throw new InvalidOperationException("This basic block already has an exit.");

            var (builder, index) = _parent.GetNewBasicBlock();
            DefaultSuccessor = index;

            return builder;
        }

        /// <summary>
        /// Creates a new basic block builder and sets <see cref="AlternativeSuccessor"/> to point to the new basic block.
        /// This method may be called if a successor is set, but not if the block already ends in a return or a branch.
        /// </summary>
        /// <param name="conditionValueIndex">The value index of the branch condition.</param>
        public BasicBlockBuilder CreateBranch(int conditionValueIndex)
        {
            if (Instructions.Count > 0 && (Instructions[Instructions.Count - 1].Operation == Opcode.Return ||
                                           Instructions[Instructions.Count - 1].Operation == Opcode.BranchIf))
            {
                throw new InvalidOperationException("This basic block already ends in a return or branch.");
            }

            var (builder, index) = _parent.GetNewBasicBlock();
            AlternativeSuccessor = index;

            // AppendInstruction would incorrectly check for an exit condition
            Instructions.Add(new Instruction(Opcode.BranchIf, conditionValueIndex, 0, 0));

            return builder;
        }

        /// <summary>
        /// Sets the default successor to equal the given basic block index.
        /// This method may not be called if the successor is already set.
        /// </summary>
        /// <param name="index">The basic block index. This is only checked when the basic block graph is built.</param>
        public void SetSuccessor(int index)
        {
            if (HasDefinedExitBehavior)
                throw new InvalidOperationException("This basic block already has an exit.");

            DefaultSuccessor = index;
        }
    }
}
