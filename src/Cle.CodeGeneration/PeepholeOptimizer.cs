using System;
using System.Collections.Generic;
using Cle.CodeGeneration.Lir;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// Peephole optimization of LIR for the x64 architecture.
    /// </summary>
    internal static class PeepholeOptimizer<TRegister>
        where TRegister: struct, Enum
    {
        /// <summary>
        /// Optimizes the given LIR.
        /// </summary>
        /// <param name="method">The method to optimize. The blocks will be mutated in place.</param>
        public static void Optimize([NotNull] LowMethod<TRegister> method)
        {
            // As an extension to a basic peephole optimizer, count the uses of locals
            // TODO: Get a pooled array
            var uses = new int[method.Locals.Count];
            CountUses(method, uses);

            // Optimize each basic block on its own
            // TODO: Add more aggressive patterns that are only enabled on optimizing builds
            foreach (var block in method.Blocks)
            {
                OptimizeBlock(block.Instructions, uses);
            }
        }

        private static void CountUses([NotNull] LowMethod<TRegister> method, [NotNull] int[] uses)
        {
            // A variable is counted as used if it has a fixed storage location
            // For example, the Return op implicitly uses the local stored in the return register
            for (var i = 0; i < method.Locals.Count; i++)
            {
                if (method.Locals[i].Location.IsSet)
                    uses[i] += 100; // Distinguish from ordinary locals
            }

            // Go through the instructions and count reads (not writes)
            foreach (var block in method.Blocks)
            {
                foreach (var inst in block.Instructions)
                {
                    if (inst.UsesLeft)
                        uses[inst.Left]++;
                    if (inst.UsesRight)
                        uses[inst.Right]++;
                }
            }
        }

        private static void OptimizeBlock([NotNull] List<LowInstruction> block, [NotNull] int[] localUses)
        {
            var currentPos = 0;
            while (currentPos < block.Count)
            {
                // Do not advance if an optimization was applied, since another optimization may now be available
                if (!PeepBasic(block, currentPos, localUses))
                    currentPos++;
            }
        }

        /// <summary>
        /// Applies peephole patterns that are important for code quality even in non-optimizing builds.
        /// These fix up silly patterns created by lowering and help out register allocation.
        /// </summary>
        private static bool PeepBasic([NotNull] List<LowInstruction> block, int currentPos, [NotNull] int[] localUses)
        {
            var current = block[currentPos]; // TODO: This might be an expensive copy
            var instructionsLeft = block.Count - currentPos - 1;

            if (instructionsLeft >= 1)
            {
                var next = block[currentPos + 1];

                if (next.Op == LowOp.Move)
                {
                    if (current.Op == LowOp.LoadInt && next.Left == current.Dest && localUses[current.Dest] == 1)
                    {
                        // When #0 is used only in the move instruction
                        // ----
                        // Load val -> #0
                        // Move #0 -> #1
                        // ----
                        // Load val -> #1
                        block[currentPos + 1] = new LowInstruction(LowOp.LoadInt, next.Dest, 0, 0, current.Data);
                        block.RemoveAt(currentPos);
                        return true;
                    }
                    else if (current.Op == LowOp.SetIfEqual && next.Left == current.Dest && localUses[current.Dest] == 1)
                    {
                        // When #0 is used only in the move instruction
                        // ----
                        // SetIfEqual -> #0
                        // Move #0 -> #1
                        // ----
                        // SetIfEqual -> #1
                        block[currentPos + 1] = new LowInstruction(LowOp.SetIfEqual, next.Dest, 0, 0, 0);
                        block.RemoveAt(currentPos);
                        return true;
                    }
                }
            }

            if (instructionsLeft >= 3
                && current.Op == LowOp.Compare
                && block[currentPos + 1].Op == LowOp.SetIfEqual
                && block[currentPos + 2].Op == LowOp.Test
                && block[currentPos + 3].Op == LowOp.JumpIfNotEqual)
            {
                // This pattern is created by lowering because the high IR does not explicitly distinguish
                // whether the comparison result is a temporary or a proper variable.
                // ----
                // Compare #0, #1
                // SetIfEqual -> #2
                // Test #2
                // JumpIfNotEqual dest
                // ----
                // Compare #0, #1
                // (SetIfEqual #2, if #2 is used later on)
                // JumpIfEqual dest

                if (localUses[block[currentPos + 1].Dest] == 1)
                {
                    // The comparison result is not used, both SetIfEqual and Test may be omitted
                    block[currentPos + 3] = new LowInstruction(LowOp.JumpIfEqual, block[currentPos + 3].Dest, 0, 0, 0);
                    block.RemoveAt(currentPos + 2);
                    block.RemoveAt(currentPos + 1);
                }
                else
                {
                    // The Test may be omitted as the comparison result is still in processor flags
                    block[currentPos + 3] = new LowInstruction(LowOp.JumpIfEqual, block[currentPos + 3].Dest, 0, 0, 0);
                    block.RemoveAt(currentPos + 2);
                }
            }

            return false;
        }
    }
}
