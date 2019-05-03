using System;
using System.Collections.Immutable;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// A builder for <see cref="BasicBlock"/> instances.
    /// Instance methods allow adding instructions and creating/setting successor blocks.
    /// Additionally, once a basic block is guaranteed to exit, all operations become no-ops to omit dead code.
    /// </summary>
    public class BasicBlockBuilder
    {
        internal ImmutableList<Instruction>.Builder Instructions { get; } = ImmutableList<Instruction>.Empty.ToBuilder();

        internal ImmutableList<Phi>.Builder Phis { get; } = ImmutableList<Phi>.Empty.ToBuilder();

        /// <summary>
        /// Gets the index of this basic block.
        /// </summary>
        public int Index { get; }

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

        private readonly BasicBlockGraphBuilder _parent;

        internal BasicBlockBuilder(BasicBlockGraphBuilder parent, int index)
        {
            _parent = parent;
            Index = index;
        }

        /// <summary>
        /// Appends an instruction with the given parameters.
        /// If this block already has a defined exit, does nothing.
        /// </summary>
        public void AppendInstruction(Opcode opcode, ulong left, ushort right, ushort destination)
        {
            if (HasDefinedExitBehavior)
                return;

            Instructions.Add(new Instruction(opcode, left, right, destination));
        }

        /// <summary>
        /// Adds a Phi node with the specified destination local index and source local indices.
        /// </summary>
        /// <param name="destination">The local that will receive the merged value from the Phi.</param>
        /// <param name="operands">The operand locals to the Phi.</param>
        public void AddPhi(int destination, ImmutableList<int> operands)
        {
            Phis.Add(new Phi(destination, operands));
        }

        /// <summary>
        /// Creates a new basic block builder and sets <see cref="DefaultSuccessor"/> to point to the new basic block.
        /// This method may not be called if the successor is already set.
        /// If this block has defined exit behavior, this only creates a builder and does not set the successor.
        /// </summary>
        public BasicBlockBuilder CreateSuccessorBlock()
        {
            if (DefaultSuccessor != -1)
                throw new InvalidOperationException("The default successor is already set.");
            if (HasDefinedExitBehavior)
                return _parent.GetNewBasicBlock();

            var builder = _parent.GetNewBasicBlock();
            DefaultSuccessor = builder.Index;

            return builder;
        }

        /// <summary>
        /// Creates a new basic block builder and sets <see cref="AlternativeSuccessor"/> to point to the new basic block.
        /// This method may be called if a successor is set, but not if the block already ends in a return or a branch.
        /// </summary>
        /// <param name="conditionValueIndex">The value index of the branch condition.</param>
        public BasicBlockBuilder CreateBranch(ushort conditionValueIndex)
        {
            if (Instructions.Count > 0 && (Instructions[Instructions.Count - 1].Operation == Opcode.Return ||
                                           Instructions[Instructions.Count - 1].Operation == Opcode.BranchIf))
            {
                throw new InvalidOperationException("This basic block already ends in a return or branch.");
            }

            var builder = _parent.GetNewBasicBlock();
            AlternativeSuccessor = builder.Index;

            // AppendInstruction would incorrectly check for an exit condition
            Instructions.Add(new Instruction(Opcode.BranchIf, conditionValueIndex, 0, 0));

            return builder;
        }

        /// <summary>
        /// Sets the default successor to equal the given basic block index.
        /// This method may not be called if the default successor is already set.
        /// If the block already has defined exit behavior, this call does nothing.
        /// </summary>
        /// <param name="index">The basic block index. This is only checked when the basic block graph is built.</param>
        public void SetSuccessor(int index)
        {
            if (DefaultSuccessor != -1)
                throw new InvalidOperationException("The default successor is already set.");
            if (HasDefinedExitBehavior)
                return;

            DefaultSuccessor = index;
        }

        /// <summary>
        /// Sets the alternative successor.
        /// Use <see cref="CreateBranch"/> instead unless you need to override the target block.
        /// 
        /// This method may not be called if the alternative successor is already set.
        /// If the block already has defined exit behavior, this call does nothing.
        /// </summary>
        /// <param name="index">The basic block index. This is only checked when the basic block graph is built.</param>
        public void SetAlternativeSuccessor(int index)
        {
            if (AlternativeSuccessor != -1)
                throw new InvalidOperationException("The alternative successor is already set.");
            if (HasDefinedExitBehavior)
                return;

            AlternativeSuccessor = index;
        }
    }
}
