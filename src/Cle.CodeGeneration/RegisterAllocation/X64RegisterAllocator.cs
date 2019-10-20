using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cle.CodeGeneration.Lir;

using Interval = Cle.CodeGeneration.RegisterAllocation.Interval<Cle.CodeGeneration.Lir.X64Register>;

namespace Cle.CodeGeneration.RegisterAllocation
{
    /// <summary>
    /// Register allocator for the x64 architecture.
    /// </summary>
    /// <remarks>
    /// This is modeled after Wimmer and Franz: Linear Scan Register Allocation on SSA Form (CGO '10, ACM),
    /// with some simplifications.
    /// </remarks>
    internal static class X64RegisterAllocator
    {
        // Registers are ordered in decreasing preference with regards to two conditions:
        //   - basic registers are preferred over new registers (r8-15) for shorter encoding
        //   - volatile registers are preferred over callee-saved registers to avoid pushing/popping
        //   - additionally, rcx and rdx are preferred over rax since a lot of loads are for parameters
        // This should increase code quality but does cause extra work in allocation (more conflicts)
        private static readonly int[] s_preferredIntegerRegisterOrder =
        {
            (int)X64Register.Rcx,
            (int)X64Register.Rdx,
            (int)X64Register.Rax,
            (int)X64Register.R8,
            (int)X64Register.R9,
            (int)X64Register.Rdi,
            (int)X64Register.Rsi,
            (int)X64Register.Rbp,
            (int)X64Register.R10,
            (int)X64Register.R11,
            (int)X64Register.R12,
            (int)X64Register.R13,
            (int)X64Register.R14,
            (int)X64Register.R15,
        };

        /// <summary>
        /// Allocates registers for the given method, and returns a method instance rewritten to reference
        /// intervals instead of locals. The interval to local map is also returned.
        /// The local variable list is preserved, but the indices do not match with the instructions as
        /// before the allocation.
        /// </summary>
        /// <param name="method">
        /// The method must be in SSA form with critical edges broken.
        /// </param>
        public static (LowMethod<X64Register> allocatedMethod, AllocationInfo<X64Register> allocationInfo)
            Allocate(LowMethod<X64Register> method)
        {
            // Compute the live intervals
            var intervals = new List<Interval>(method.Locals.Count);
            var blockEnds = new int[method.Blocks.Count];
            ComputeLiveIntervals(method, intervals, blockEnds);

            // Sort the intervals by start position
            intervals.Sort();

            // Allocate registers by doing a linear scan
            DoLinearScan(intervals);

            // Rewrite the method to use interval numbers instead of local indices
            // This also resolves Phi functions by converting them into moves/swaps
            var rewrittenMethod = RewriteMethod(method, intervals, blockEnds);

            return (rewrittenMethod, new AllocationInfo<X64Register>(intervals));
        }

        /// <summary>
        /// Phase 1: Compute live intervals.
        /// Each interval maps to a single local in a contiguous region of instructions.
        /// The regions are defined by instruction counts, where the set of Phis is considered one instruction.
        /// </summary>
        /// <param name="method">The original LIR method where instructions refer to locals.</param>
        /// <param name="intervals">An empty list that will be populated by the computed intervals.</param>
        /// <param name="blockEnds">
        /// An empty array that has an element for each block.
        /// This will be populated with the last instruction index (inclusive) of each basic block.
        /// </param>
        private static void ComputeLiveIntervals(LowMethod<X64Register> method,
            List<Interval> intervals, int[] blockEnds)
        {
            // We must start from the maximum index as we traverse the blocks in reverse order
            // NOTE: This complicates the instruction counting quite a bit, so be careful!
            var instIndex = 0;
            foreach (var block in method.Blocks)
                instIndex += block.Instructions.Count + 1;

            // TODO: Is this a sensible design correctness/performance-wise?
            var latestIntervalForLocal = new int[method.Locals.Count];
            for (var i = 0; i < latestIntervalForLocal.Length; i++)
                latestIntervalForLocal[i] = -1;

            // A temporary data structure - this is not updated by the loop header handling
            var liveIn = new SortedSet<int>[method.Blocks.Count];
            for (var i = 0; i < liveIn.Length; i++)
                liveIn[i] = new SortedSet<int>();

            // The reverse order is used because it typically sees block successors first
            for (var blockIndex = method.Blocks.Count - 1; blockIndex >= 0; blockIndex--)
            {
                var block = method.Blocks[blockIndex];
                var blockEnd = instIndex - 1;
                blockEnds[blockIndex] = blockEnd;
                var blockStart = blockEnd - block.Instructions.Count;
                var live = liveIn[blockIndex];

                // Initialize the live set to contain all locals that are live at the beginning of some
                // succeeding block, and all locals used in Phis of the succeeding blocks
                if (block.Successors is object)
                {
                    foreach (var succ in block.Successors)
                    {
                        live.UnionWith(liveIn[succ]);

                        var phis = method.Blocks[succ].Phis;
                        if (phis is null)
                            continue;

                        var phiPosition = GetPhiOperandIndex(blockIndex, method.Blocks[succ]);
                        foreach (var phi in phis)
                            live.Add(phi.Operands[phiPosition]);
                    }
                }

                // Create an interval for each live local
                // TODO: Consider merging adjacent intervals of a single local
                foreach (var liveLocal in live)
                {
                    AddIntervalForLocal(liveLocal, blockStart, blockEnd);
                }

                // Then go through the instructions in reverse order
                for (var j = block.Instructions.Count - 1; j >= 0; j--)
                {
                    var inst = block.Instructions[j];
                    instIndex--;

                    // Since the LIR is in SSA form, the output operand is not live before this instruction
                    if (inst.UsesDest)
                    {
                        if (latestIntervalForLocal[inst.Dest] == -1)
                        {
                            // If the result local is not used anywhere, we need to create a short interval here
                            AddIntervalForLocal(inst.Dest, instIndex, instIndex);
                        }
                        else
                        {
                            intervals[latestIntervalForLocal[inst.Dest]].Start = instIndex;
                        }

                        live.Remove(inst.Dest);
                    }

                    // Input operands are defined before their uses, so we only need to add an interval
                    // if the local is not yet live. Initially we set the lifetime to start at the start
                    // of the block, but this may be shortened if the local is defined in this block.
                    if (inst.UsesLeft && !live.Contains(inst.Left))
                    {
                        AddIntervalForLocal(inst.Left, blockStart, instIndex);
                        live.Add(inst.Left);
                    }

                    // The right-hand operand may be set to -1 to signal a constant (immediate) argument
                    if (inst.UsesRight && inst.Right >= 0 && !live.Contains(inst.Right))
                    {
                        AddIntervalForLocal(inst.Right, blockStart, instIndex);
                        live.Add(inst.Right);
                    }

                    // Some instructions (e.g. calls) trash one or more registers
                    AddX64SpecificIntervals(inst, instIndex);
                }

                // Remove Phi outputs from the live set
                if (block.Phis is object)
                {
                    foreach (var phi in block.Phis)
                    {
                        intervals[latestIntervalForLocal[phi.Destination]].Use(instIndex - 1);
                        live.Remove(phi.Destination);
                    }
                }

                // The set of Phis is a single instruction (even if empty)
                instIndex--;
                Debug.Assert(instIndex == blockStart);

                // If this block is a loop header (has a predecessor with greater block index, or is
                // its own predecessor), extend the lifetimes of locals that are live for the entire loop
                foreach (var predIndex in block.Predecessors)
                {
                    if (predIndex < blockIndex)
                        continue;

                    foreach (var liveLocal in live)
                    {
                        AddIntervalForLocal(liveLocal, blockStart, blockEnds[predIndex]);
                    }
                }
            }

            Debug.Assert(instIndex == 0);

            // LOCAL HELPER METHODS

            void AddIntervalForLocal(int localIndex, int start, int end)
            {
                // If there already is an adjacent interval, update it instead of creating another
                // TODO: Is looking up in this cache enough?
                if (latestIntervalForLocal[localIndex] >= 0)
                {
                    var existing = intervals[latestIntervalForLocal[localIndex]];
                    if (existing.Start <= end + 1 && existing.End >= start - 1)
                    {
                        existing.Use(start);
                        existing.Use(end);
                        return;
                    }
                }

                // Else, create a new interval
                intervals.Add(new Interval()
                {
                    LocalIndex = localIndex,
                    Register = method.Locals[localIndex].RequiredLocation.Register,
                    Start = start,
                    End = end
                });
                latestIntervalForLocal[localIndex] = intervals.Count - 1;
            }

            void AddX64SpecificIntervals(in LowInstruction inst, int instIndex)
            {
                // Prevent X64 trashing the right operand of subtraction/shift (see associated unit test)
                // except when the right operand is a constant.
                // Additionally, the right operand of shift is fixed to RCX, but Lowering has handled that
                if ((inst.Op == LowOp.IntegerSubtract || inst.Op == LowOp.ShiftLeft || inst.Op == LowOp.ShiftArithmeticRight)
                    && inst.Right >= 0)
                {
                    intervals[latestIntervalForLocal[inst.Right]].Use(instIndex + 1);
                }

                // In integer division, the dividend is stored in RDX:RAX.
                // The lower part is already handled since the source is a fixed temporary,
                // but we must prevent RDX from being used for the divisor.
                if (inst.Op == LowOp.IntegerDivide || inst.Op == LowOp.IntegerModulo)
                {
                    intervals.Add(new Interval { Start = instIndex - 1, End = instIndex, Register = X64Register.Rdx });
                }

                // Calls trash some registers, so we need to add intervals for them
                if (inst.Op == LowOp.Call || inst.Op == LowOp.CallImported)
                {
                    // RAX is already reserved as the call result is stored in a local
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.Rcx });
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.Rdx });
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R8 });
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R9 });
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R10 });
                    intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R11 });
                }
            }
        }

        /// <summary>
        /// phiBlock.Predecessors.IndexOf(blockIndex)
        /// </summary>
        private static int GetPhiOperandIndex(int blockIndex, LowBlock phiBlock)
        {
            for (var j = 0; j < phiBlock.Predecessors.Count; j++)
            {
                if (phiBlock.Predecessors[j] == blockIndex)
                    return j;
            }

            throw new InvalidOperationException("Successors and predecessors do not match.");
        }

        /// <summary>
        /// Phase 2: Allocate storage locations for intervals.
        /// This is a standard linear scan algorithm.
        /// </summary>
        /// <param name="intervals">
        /// The list of intervals with lifetimes and possible register requirements.
        /// This list must be sorted by increasing start position.
        /// </param>
        private static void DoLinearScan(List<Interval<X64Register>> intervals)
        {
            var live = new List<Interval>();
            var freeSince = new int[(int)X64Register.Count];
            var registerUsers = new int[(int)X64Register.Count];
            for (var i = 0; i < registerUsers.Length; i++)
                registerUsers[i] = -1;

            // Loop through the intervals in order of start position
            for (var intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
            {
                var interval = intervals[intervalIndex];

                // Remove intervals that have ended before (or at) this position
                for (var i = live.Count - 1; i >= 0; i--)
                {
                    if (live[i].End <= interval.Start)
                    {
                        var freedRegister = (int)live[i].Register;
                        freeSince[freedRegister] = live[i].End;
                        registerUsers[freedRegister] = -1;
                        live.RemoveAt(i);
                    }
                }

                // Add the current interval to the active list
                // TODO: Consider sorting the live list by interval end
                live.Add(interval);

                // Find a register for the current interval, possibly reallocating
                // another, blocking interval
                AllocateInterval(intervalIndex, intervals, registerUsers, freeSince);
            }
        }

        private static void AllocateInterval(int intervalIndex, List<Interval> intervals,
            int[] registerUsers, int[] freeSince)
        {
            // Register requirements are always respected
            if (intervals[intervalIndex].Register > X64Register.Invalid)
            {
                var blocker = registerUsers[(int)intervals[intervalIndex].Register];

                ReserveRegister((int)intervals[intervalIndex].Register, intervalIndex, registerUsers, freeSince);

                if (blocker == -1)
                {
                    // The register is free - no problem
                    return;
                }
                else
                {
                    // The register is blocked - we have to reallocate the blocking register.
                    // If the blocking register also has a requirement, it WON'T be respected -
                    // ergo, intervals with requirements should be kept as short as possible.
                    foreach (var reg in s_preferredIntegerRegisterOrder)
                    {
                        if (registerUsers[reg] == -1 && freeSince[reg] <= intervals[blocker].Start)
                        {
                            intervals[blocker].Register = (X64Register)reg;
                            ReserveRegister(reg, blocker, registerUsers, freeSince);
                            return;
                        }
                    }

                    throw new NotImplementedException("Spilling a blocker to stack");
                }
            }

            // Else, get the first free register
            foreach (var reg in s_preferredIntegerRegisterOrder)
            {
                if (registerUsers[reg] == -1)
                {
                    ReserveRegister(reg, intervalIndex, registerUsers, freeSince);
                    intervals[intervalIndex].Register = (X64Register)reg;
                    return;
                }
            }

            // If that failed, we need to spill the interval onto stack
            throw new NotImplementedException("Spilling to stack");
        }

        private static void ReserveRegister(int registerIndex, int intervalIndex, int[] registerUsers, int[] freeSince)
        {
            registerUsers[registerIndex] = intervalIndex;
            freeSince[registerIndex] = int.MaxValue;
        }

        /// <summary>
        /// Phase 3: Rewrite the instructions to reference intervals and convert Phis to moves.
        /// </summary>
        /// <param name="original">The original LIR method that was passed to the allocator.</param>
        /// <param name="intervals">The list of intervals with allocation decisions done.</param>
        /// <param name="blockEnds">
        /// The block ends computed by
        /// <see cref="ComputeLiveIntervals(LowMethod{X64Register}, List{Interval{X64Register}}, int[])"/>.
        /// </param>
        private static LowMethod<X64Register> RewriteMethod(LowMethod<X64Register> original,
            List<Interval> intervals, int[] blockEnds)
        {
            var result = new LowMethod<X64Register>(original.Locals, new List<LowBlock>(original.Blocks.Count), original.IsLeafMethod);

            // Replace instruction operands with references to intervals instead of locals
            var instIndex = 0;
            for (var blockIndex = 0; blockIndex < original.Blocks.Count; blockIndex++)
            {
                // TODO: Account for instructions emitted by Phi resolution in the capacity calculation
                var oldBlock = original.Blocks[blockIndex];
                var newBlock = new LowBlock(new List<LowInstruction>(oldBlock.Instructions.Count))
                {
                    Phis = oldBlock.Phis, // This is required in ConvertPhisToMoves but then nulled out
                    Predecessors = oldBlock.Predecessors,
                    Successors = oldBlock.Successors
                };

                instIndex++;

                foreach (var inst in oldBlock.Instructions)
                {
                    newBlock.Instructions.Add(new LowInstruction(inst.Op,
                        inst.UsesDest ? ConvertLocalToInterval(inst.Dest, intervals, instIndex) : inst.Dest,
                        inst.UsesLeft ? ConvertLocalToInterval(inst.Left, intervals, instIndex) : inst.Left,
                        inst.UsesRight && inst.Right >= 0 ? ConvertLocalToInterval(inst.Right, intervals, instIndex) : inst.Right,
                        inst.Data));
                    instIndex++;
                }

                result.Blocks.Add(newBlock);
            }

            // Resolve Phi functions
            ConvertPhisToMoves(result, intervals, blockEnds);

            return result;
        }


        private static int ConvertLocalToInterval(int local, List<Interval> intervals, int position)
        {
            // TODO TODO TODO: This is, as used here, O(N^2) with large N!

            // The intervals are sorted by start position
            for (var i = 0; i < intervals.Count; i++)
            {
                var interval = intervals[i];

                if (interval.LocalIndex == local && interval.Start <= position && interval.End >= position)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("No interval matching the local.");
        }

        private static void ConvertPhisToMoves(LowMethod<X64Register> method, List<Interval> intervals, int[] blockEnds)
        {
            // This also handles different locations between basic blocks
            var movesToDo = new List<(int fromInterval, int toInterval)>();

            for (var blockIndex = 0; blockIndex < method.Blocks.Count; blockIndex++)
            {
                var instIndex = blockIndex == 0 ? 0 : blockEnds[blockIndex - 1] + 1;
                var block = method.Blocks[blockIndex];
                if (block.Predecessors is null)
                {
                    continue;
                }

                for (var i = 0; i < block.Predecessors.Count; i++)
                {
                    movesToDo.Clear();

                    var predIndex = block.Predecessors[i];
                    var phiOperandIndex = GetPhiOperandIndex(predIndex, block);

                    // Go through all intervals live at the start of this block
                    for (var intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
                    {
                        var interval = intervals[intervalIndex];

                        if (interval.Start == instIndex)
                        {
                            // If the interval starts at the very start of this block, it may be defined by a Phi
                            // Find the Phi and add a move to resolve
                            var foundPhi = false;
                            if (block.Phis is object)
                            {
                                foreach (var phi in block.Phis)
                                {
                                    if (phi.Destination == interval.LocalIndex)
                                    {
                                        var source = ConvertLocalToInterval(phi.Operands[phiOperandIndex], intervals,
                                            blockEnds[predIndex]);
                                        var dest = ConvertLocalToInterval(phi.Destination, intervals, instIndex);

                                        movesToDo.Add((source, dest));
                                        foundPhi = true;
                                        break;
                                    }
                                }
                            }

                            if (!foundPhi)
                            {
                                // Else, the interval continues the lifetime of an existing local
                                // Since its location may have changed we need to emit a move
                                // TODO: Skip redundant moves
                                var source = ConvertLocalToInterval(interval.LocalIndex, intervals, blockEnds[predIndex]);
                                movesToDo.Add((source, intervalIndex));
                            }
                        }
                    }

                    // Resolve the moves
                    var pred = method.Blocks[predIndex];
                    if (pred.Successors.Count == 1)
                    {
                        // If the predecessor only has a single successor, emit the copies at the end of it
                        // As a sanity check, we expect the jump instruction to be the last instruction of the block
                        if (pred.Instructions[pred.Instructions.Count - 1].Op != LowOp.Jump)
                            throw new InvalidOperationException("Expected unconditional jump at the end of block.");

                        EmitRegisterMoves(movesToDo, pred.Instructions, pred.Instructions.Count - 1, intervals);
                    }
                    else
                    {
                        // Else, emit the copies at the start of this basic block
                        // This can only succeed if this basic block has no other predecessors
                        if (block.Predecessors.Count > 1)
                            throw new InvalidOperationException("Critical edges must be split.");

                        EmitRegisterMoves(movesToDo, block.Instructions, 0, intervals);
                    }
                }

                // The Phi list is neither needed nor relevant any more
                block.Phis = null;
            }
        }

        /// <summary>
        /// Emits necessary register moves/swaps at the requested position.
        /// </summary>
        /// <param name="movesToDo">The list of permutations to do. The order is not respected.</param>
        /// <param name="instructions">The instruction list that will be modified.</param>
        /// <param name="insertionPos">The index to the instruction list where the moves will be added.</param>
        /// <param name="locals">The list of locals with allocation decisions.</param>
        private static void EmitRegisterMoves(List<(int from, int to)> movesToDo,
            List<LowInstruction> instructions, int insertionPos, List<Interval> intervals)
        {
            for (var i = 0; i < movesToDo.Count; i++)
            {
                // The trivial case - this can also arise from conflict fixups
                var fromPos = intervals[movesToDo[i].from].Register;
                var toPos = intervals[movesToDo[i].to].Register;

                if (fromPos == toPos)
                    continue;

                // If this does not conflict with the other permutations, we can just emit a move.
                // Else, we need to do a swap.
                var conflicts = false;
                for (var j = i + 1; j < movesToDo.Count; j++)
                {
                    if (intervals[movesToDo[j].from].Register == toPos)
                        conflicts = true;
                }

                if (conflicts)
                {
                    instructions.Insert(insertionPos,
                        new LowInstruction(LowOp.Swap, 0, movesToDo[i].from, movesToDo[i].to, 0));
                    insertionPos++;

                    // Fix all the upcoming moves to have the correct source
                    for (var j = i + 1; j < movesToDo.Count; j++)
                    {
                        if (intervals[movesToDo[j].from].Register == toPos)
                            movesToDo[j] = (movesToDo[i].from, movesToDo[j].to);
                    }
                }
                else
                {
                    instructions.Insert(insertionPos,
                        new LowInstruction(LowOp.Move, movesToDo[i].to, movesToDo[i].from, 0, 0));
                    insertionPos++;
                }
            }
        }
    }
}
