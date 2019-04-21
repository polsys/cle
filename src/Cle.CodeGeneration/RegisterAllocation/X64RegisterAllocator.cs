using System;
using System.Collections.Generic;
using Cle.CodeGeneration.Lir;
using JetBrains.Annotations;

using Interval = Cle.CodeGeneration.RegisterAllocation.Interval<Cle.CodeGeneration.Lir.X64Register>;

namespace Cle.CodeGeneration.RegisterAllocation
{
    /// <summary>
    /// Register allocator for the x64 architecture.
    /// </summary>
    internal static class X64RegisterAllocator
    {
        // Registers are ordered in decreasing preference with regards to two conditions:
        //   - basic registers are preferred over new registers (r8-15) for shorter encoding
        //   - volatile registers are preferred over callee-saved registers to avoid pushing/popping
        // This should increase code quality but does cause extra work in allocation (more conflicts)
        private static readonly int[] s_preferredIntegerRegisterOrder =
        {
            (int)X64Register.Rax,
            (int)X64Register.Rcx,
            (int)X64Register.Rdx,
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
            Allocate([NotNull] LowMethod<X64Register> method)
        {
            /* TODO: This is a quick-and-dirty linear scan register allocator for the initial bring-up!
             * Features:
             *   - does NOT support spilling to stack (!)
             *   - does NOT support splitting live intervals
             *   - is NOT mathematically proven correct
             *   - handles Phis by merging the operand and result locals for the purposes of allocation
             */
            
            // Compute the live intervals
            var intervals = new List<Interval>(method.Locals.Count);
            var localToAllocatedLocalMap = new int[method.Locals.Count];
            for (var i = 0; i < method.Locals.Count; i++)
            {
                intervals.Add(new Interval { LocalIndex = i, Register = method.Locals[i].RequiredLocation.Register });
                localToAllocatedLocalMap[i] = i;
            }

            ComputeLiveIntervals(method, intervals, localToAllocatedLocalMap);

            // Sort the intervals by start position
            for (var i = intervals.Count - 1; i >= 0; i--)
            {
                if (intervals[i] is null)
                    intervals.RemoveAt(i);
            }
            intervals.Sort();

            // Allocate registers by doing a linear scan
            var live = new List<Interval>();
            var freeSince = new int[(int)X64Register.Count];
            var registerUsers = new int[(int)X64Register.Count];
            for (var i = 0; i < registerUsers.Length; i++)
                registerUsers[i] = -1;

            for (var intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
            {
                var interval = intervals[intervalIndex];

                // Remove intervals that have ended
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

                // Find a register for the current interval
                AllocateInterval(intervalIndex, intervals, registerUsers, freeSince);
            }

            // Return the allocation decisions
            // TODO: Actually rewrite the method - this is a temporary WIP solution
            var tempIntervalList = new Interval<X64Register>[method.Locals.Count];
            foreach (var interval in intervals)
            {
                if (interval.LocalIndex >= 0)
                {
                    tempIntervalList[interval.LocalIndex] = interval;
                }
            }
            for (var i = 0; i < localToAllocatedLocalMap.Length; i++)
            {
                var copySource = tempIntervalList[localToAllocatedLocalMap[i]];
                tempIntervalList[i] = new Interval<X64Register>()
                {
                    Start = copySource.Start,
                    End = copySource.End,
                    LocalIndex = i,
                    Register = copySource.Register
                };
            }

            var allocationInfo = new AllocationInfo<X64Register>(new List<Interval<X64Register>>(tempIntervalList));
            return (method, allocationInfo);
        }

        private static void ComputeLiveIntervals(LowMethod<X64Register> method, List<Interval> intervals, int[] localToAllocatedLocalMap)
        {
            var instIndex = 0;
            foreach (var block in method.Blocks)
            {
                // Phis: each phi is a use location for each operand (and destination)
                if (!(block.Phis is null))
                {
                    foreach (var phi in block.Phis)
                    {
                        intervals[phi.Destination].Use(instIndex);
                        foreach (var op in phi.Operands)
                        {
                            intervals[op].Use(instIndex);

                            // Now the phi operand has the same register as its destination
                            var parent = localToAllocatedLocalMap[phi.Destination];
                            localToAllocatedLocalMap[op] = parent;

                            // We also need to update all the locals that depend on the phi operand
                            // THIS IS O(N^2), but for high N this algorithm is expected to fail anyways
                            for (var j = 0; j < localToAllocatedLocalMap.Length; j++)
                            {
                                if (localToAllocatedLocalMap[j] == op)
                                    localToAllocatedLocalMap[j] = parent;
                            }
                        }
                    }
                }

                instIndex++;

                // Then go through the instructions
                foreach (var inst in block.Instructions)
                {
                    if (inst.UsesLeft)
                        intervals[inst.Left].Use(instIndex);
                    if (inst.UsesRight)
                        intervals[inst.Right].Use(instIndex);
                    if (inst.UsesDest)
                        intervals[inst.Dest].Use(instIndex);

                    // Prevent X64 trashing the right operand of subtraction (see associated unit test)
                    if (inst.Op == LowOp.IntegerSubtract)
                        intervals[inst.Right].Use(instIndex + 1);

                    // Calls trash some registers, so we need to add intervals for them
                    if (inst.Op == LowOp.Call)
                    {
                        // RAX is already reserved as the call result is stored in a local
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.Rcx });
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.Rdx });
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R8 });
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R9 });
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R10 });
                        intervals.Add(new Interval { Start = instIndex, End = instIndex, Register = X64Register.R11 });
                    }

                    instIndex++;
                }
            }

            // Merge the intervals of Phi operands
            // TODO: This does not handle register requirements of operands gracefully (there shouldn't be any)
            for (var i = localToAllocatedLocalMap.Length - 1; i >= 0; i--)
            {
                if (localToAllocatedLocalMap[i] == i)
                    continue;

                intervals[localToAllocatedLocalMap[i]].MergeWith(intervals[i]);
                intervals[i] = null;
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

                    throw new NotImplementedException("How to proceed with this case?");
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

            // If that failed, we need to spill a local
            throw new NotImplementedException("Spilling locals");
        }

        private static void ReserveRegister(int registerIndex, int intervalIndex, int[] registerUsers, int[] freeSince)
        {
            registerUsers[registerIndex] = intervalIndex;
            freeSince[registerIndex] = int.MaxValue;
        }
    }
}
