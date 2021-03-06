﻿using System;
using System.Collections.Generic;
using Cle.CodeGeneration.Lir;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// Peephole optimization of LIR for the x64 architecture.
    /// This optimization pass is run before register allocation and is therefore limited to
    /// optimizations that preserve SSA form.
    /// </summary>
    internal static class PeepholeOptimizer<TRegister>
        where TRegister: struct, Enum
    {
        private const int FixedLocationSentinel = 100000;

        /// <summary>
        /// Optimizes the given LIR.
        /// </summary>
        /// <param name="method">The method to optimize. The blocks will be mutated in place.</param>
        public static void Optimize(LowMethod<TRegister> method)
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

        private static void CountUses(LowMethod<TRegister> method, int[] uses)
        {
            // A variable is counted as used if it has a fixed storage location
            // For example, the Return op implicitly uses the local stored in the return register
            for (var i = 0; i < method.Locals.Count; i++)
            {
                if (method.Locals[i].RequiredLocation.IsSet)
                    uses[i] += FixedLocationSentinel; // Distinguish from ordinary locals
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

        private static void OptimizeBlock(List<LowInstruction> block, int[] localUses)
        {
            var currentPos = 0;
            while (currentPos < block.Count)
            {
                // Do not advance if an optimization was applied, since another optimization may now be available
                if (!ApplyBasicPatterns(block, currentPos, localUses))
                    currentPos++;
            }
        }

        /// <summary>
        /// Applies peephole patterns that are important for code quality even in non-optimizing builds.
        /// These fix up silly patterns created by lowering and help out register allocation.
        /// </summary>
        private static bool ApplyBasicPatterns(List<LowInstruction> block, int currentPos, int[] localUses)
        {
            var current = block[currentPos]; // TODO: This might be an expensive copy
            var instructionsLeft = block.Count - currentPos - 1;

            if (instructionsLeft >= 1)
            {
                var next = block[currentPos + 1];

                if (next.Op == LowOp.Move &&
                    PeepMovePrecededByLoadOrMove(in current, in next, block, localUses, currentPos))
                {
                    return true;
                }

                if (current.Op == LowOp.LoadInt &&
                    PeepLoadConvertibleToImmediate(in current, in next, block, localUses, currentPos))
                {
                    return true;
                }
            }

            if (instructionsLeft >= 2 &&
                IsConditionalSet(current.Op) &&
                PeepRedundancyAfterConditionalSet(in current, block, localUses, currentPos))
            {
                return true;
            }

            return false;
        }

        private static bool PeepMovePrecededByLoadOrMove(in LowInstruction current, in LowInstruction next,
            List<LowInstruction> block, int[] localUses, int currentPos)
        {
            if (next.Left == current.Dest && localUses[current.Dest] == 1)
            {
                if (current.Op == LowOp.LoadInt)
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
                else if (current.Op == LowOp.Move)
                {
                    // Same as above but with Move instead of Load
                    // This pattern occurs a lot in method calls
                    block[currentPos + 1] = new LowInstruction(LowOp.Move, next.Dest, current.Left, 0, 0);
                    block.RemoveAt(currentPos);
                    return true;
                }
                else if (IsConditionalSet(current.Op))
                {
                    // When #0 is used only in the move instruction
                    // ----
                    // SetIfXXX -> #0
                    // Move #0 -> #1
                    // ----
                    // SetIfXXX -> #1
                    block[currentPos + 1] = new LowInstruction(current.Op, next.Dest, 0, 0, 0);
                    block.RemoveAt(currentPos);
                    return true;
                }
            }

            return false;
        }

        private static bool PeepLoadConvertibleToImmediate(in LowInstruction current, in LowInstruction next,
            List<LowInstruction> block, int[] localUses, int currentPos)
        {
            if ((next.Op == LowOp.ShiftLeft || next.Op == LowOp.ShiftArithmeticRight) &&
                localUses[current.Dest] == FixedLocationSentinel + 1)
            {
                // When #2 is used only once
                // (Note that the "shift by variable" instruction requires the amount to be in RDX)
                // ----
                // Load 1234 -> #2
                // Shift #1 #2 -> #3
                // ----
                // Shift #1 1234 -> #3
                block[currentPos + 1] = new LowInstruction(next.Op, next.Dest, next.Left, -1, current.Data);
                block.RemoveAt(currentPos);
                return true;
            }

            if (ValueIsAtMost4Bytes(current.Data) && localUses[current.Dest] == 1)
            {
                if (IsArithmeticWithImmediateRight(next.Op) && current.Dest == next.Right)
                {
                    // When #2 is used only once
                    // ----
                    // Load 1234 -> #2
                    // Arithmetic #1 #2 -> #3
                    // ----
                    // Arithmetic #1 1234 -> #3
                    block[currentPos + 1] = new LowInstruction(next.Op, next.Dest, next.Left, -1, current.Data);
                    block.RemoveAt(currentPos);
                    return true;
                }

                if (IsArithmeticWithImmediateLeft(next.Op) && current.Dest == next.Left)
                {
                    // When #2 is used only once, and the arithmetic operation is commutative
                    // (That is, subtraction and compare are left out.)
                    // ----
                    // Load 1234 -> #2
                    // Arithmetic #2 #1 -> #3
                    // ----
                    // Arithmetic #1 () 1234 -> #3
                    block[currentPos + 1] = new LowInstruction(next.Op, next.Dest, next.Right, -1, current.Data);
                    block.RemoveAt(currentPos);
                    return true;
                }
            }

            return false;
        }

        private static bool PeepRedundancyAfterConditionalSet(in LowInstruction current,
            List<LowInstruction> block, int[] localUses, int currentPos)
        {
            if (block[currentPos + 1].Op == LowOp.Test && block[currentPos + 2].Op == LowOp.SetIfEqual)
            {
                // This pattern is created by lowering of a logical NOT. In HIR, only the ==, < and <= comparisons
                // are represented directly and their negations are implemented via a logical negation.
                // ----
                // SetIfEqual -> #2
                // Test #2
                // SetIfEqual -> #3
                // ----
                // (SetIfEqual -> #2, if #2 is used later on)
                // SetIfNotEqual -> #3

                var dest = block[currentPos + 2].Dest;
                block[currentPos + 2] = new LowInstruction(NegateConditional(current.Op), dest, 0, 0, 0);
                block.RemoveAt(currentPos + 1);

                if (localUses[current.Dest] == 1)
                {
                    block.RemoveAt(currentPos);
                }

                return true;
            }

            if (block[currentPos + 1].Op == LowOp.Test && block[currentPos + 2].Op == LowOp.JumpIfNotEqual)
            {
                // This pattern is created by lowering because the high IR does not explicitly distinguish
                // whether the comparison result is a temporary or a proper variable.
                // ----
                // [typically Compare #0, #1]
                // SetIfEqual -> #2
                // Test #2
                // JumpIfNotEqual dest
                // ----
                // [Compare #0, #1]
                // (SetIfEqual #2, if #2 is used later on)
                // JumpIfEqual dest

                var replacement = GetConditionalJumpFromConditionalSet(block[currentPos].Op);
                if (localUses[block[currentPos].Dest] == 1)
                {
                    // The comparison result is not used, both SetIfEqual and Test may be omitted
                    block[currentPos + 2] = new LowInstruction(replacement, block[currentPos + 2].Dest, 0, 0, 0);
                    block.RemoveAt(currentPos + 1);
                    block.RemoveAt(currentPos);
                }
                else
                {
                    // The Test may be omitted as the comparison result is still in processor flags
                    block[currentPos + 2] = new LowInstruction(replacement, block[currentPos + 2].Dest, 0, 0, 0);
                    block.RemoveAt(currentPos + 1);
                }
            }

            return false;
        }

        private static bool ValueIsAtMost4Bytes(ulong value)
        {
            // Take the sign extension into account
            return (long)value >= int.MinValue && (long)value <= int.MaxValue;
        }

        private static bool IsArithmeticWithImmediateRight(LowOp op)
        {
            return op == LowOp.IntegerAdd || op == LowOp.IntegerSubtract || op == LowOp.IntegerMultiply || op == LowOp.Compare;
        }

        private static bool IsArithmeticWithImmediateLeft(LowOp op)
        {
            return op == LowOp.IntegerAdd || op == LowOp.IntegerMultiply;
        }

        private static bool IsConditionalSet(LowOp op)
        {
            return op >= LowOp.SetIfEqual && op <= LowOp.SetIfGreaterOrEqual;
        }

        private static LowOp GetConditionalJumpFromConditionalSet(LowOp op)
        {
            switch (op)
            {
                case LowOp.SetIfEqual:
                    return LowOp.JumpIfEqual;
                case LowOp.SetIfNotEqual:
                    return LowOp.JumpIfNotEqual;
                case LowOp.SetIfLess:
                    return LowOp.JumpIfLess;
                case LowOp.SetIfLessOrEqual:
                    return LowOp.JumpIfLessOrEqual;
                case LowOp.SetIfGreater:
                    return LowOp.JumpIfGreater;
                case LowOp.SetIfGreaterOrEqual:
                    return LowOp.JumpIfGreaterOrEqual;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op.ToString());
            }
        }

        private static LowOp NegateConditional(LowOp op)
        {
            switch (op)
            {
                case LowOp.SetIfEqual:
                    return LowOp.SetIfNotEqual;
                case LowOp.SetIfNotEqual:
                    return LowOp.SetIfEqual;
                case LowOp.SetIfLess:
                    return LowOp.SetIfGreaterOrEqual;
                case LowOp.SetIfLessOrEqual:
                    return LowOp.SetIfGreater;
                case LowOp.SetIfGreater:
                    return LowOp.SetIfLessOrEqual;
                case LowOp.SetIfGreaterOrEqual:
                    return LowOp.SetIfLess;
                case LowOp.JumpIfEqual:
                    return LowOp.JumpIfNotEqual;
                case LowOp.JumpIfNotEqual:
                    return LowOp.JumpIfEqual;
                case LowOp.JumpIfLess:
                    return LowOp.JumpIfGreaterOrEqual;
                case LowOp.JumpIfLessOrEqual:
                    return LowOp.JumpIfGreater;
                case LowOp.JumpIfGreater:
                    return LowOp.JumpIfLessOrEqual;
                case LowOp.JumpIfGreaterOrEqual:
                    return LowOp.JumpIfLess;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op.ToString());
            }
        }
    }
}
