using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Cle.CodeGeneration.Lir;
using Cle.CodeGeneration.RegisterAllocation;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.RegisterAllocation
{
    internal class X64RegisterAllocatorTests
    {
        [Test]
        public void Intersecting_variables_in_single_block_have_separate_registers()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Void, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(LowOp.LoadInt, 2, 0, 0, 0), // Initialize void return
                    new LowInstruction(LowOp.Return, 0, 2, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            AssertDump(rewritten, @"
LB_0:
    LoadInt 0 0 1 -> 0
    LoadInt 0 0 1 -> 1
    Compare 0 1 0 -> 0
    LoadInt 0 0 0 -> 2
    Return 2 0 0 -> 0");

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));
            Assert.That(allocationMap.Get(2).localIndex, Is.EqualTo(2));

            Assert.That(allocationMap.Get(0).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(0).location, Is.Not.EqualTo(allocationMap.Get(1).location));
            Assert.That(allocationMap.Get(2).location.Register, Is.EqualTo(X64Register.Rax));
        }

        [Test]
        public void Non_intersecting_variables_in_single_block_use_same_register()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Void, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                    new LowInstruction(LowOp.LoadInt, 2, 0, 0, 0), // Initialize void return
                    new LowInstruction(LowOp.Return, 0, 2, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            AssertDump(rewritten, @"
LB_0:
    LoadInt 0 0 1 -> 0
    Move 0 0 0 -> 1
    Test 1 0 0 -> 0
    LoadInt 0 0 0 -> 2
    Return 2 0 0 -> 0");

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));
            Assert.That(allocationMap.Get(2).localIndex, Is.EqualTo(2));

            Assert.That(allocationMap.Get(0).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(1).location));
        }

        [TestCase(X64Register.Rax)] // Allocated by default for #0
        [TestCase(X64Register.R14)] // Never allocated in this method without a requirement
        public void Register_requirement_is_respected(X64Register required)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, required));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Compare, 0, 1, 0, 0), // Compare #1, #0
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0) // Return #0
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            AssertDump(rewritten, @"
LB_0:
    LoadInt 0 0 1 -> 0
    Move 0 0 0 -> 1
    Compare 1 0 0 -> 0
    Return 0 0 0 -> 0");

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));

            Assert.That(allocationMap.Get(0).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.Register, Is.EqualTo(required));
        }

        [Test]
        public void Single_phi_in_a_loop()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.IntegerAdd, 2, 0, 1, 0), // Add #0, #1 -> #2
                    new LowInstruction(LowOp.Jump, 0, 0, 0, 0)
                },
                Phis = new[] { new Phi(0, ImmutableList<int>.Empty.Add(2)) },
                Predecessors = new int[] { 0 },
                Successors = new int[] { 0 }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            // #0 and #2 have the same register, therefore there is no move instruction
            AssertDump(rewritten, @"
LB_0:
    LoadInt 0 0 1 -> 1
    IntegerAdd 0 1 0 -> 2
    Jump 0 0 0 -> 0");

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));

            Assert.That(allocationMap.Get(0).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(2).location));
            Assert.That(allocationMap.Get(0).location, Is.Not.EqualTo(allocationMap.Get(1).location));
        }

        [Test]
        public void Complex_phi_chain_is_not_allocated_the_same_register()
        {
            // void Swap() {
            //   int32 a = 10;
            //   int32 b = 11;
            //   while (a < b) {
            //      int32 temp = a;
            //      a = b;
            //      b = temp;
            //   }
            // }
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Void, X64Register.Rax));

            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 10), // Load 10 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 11), // Load 11 -> #1
                    new LowInstruction(LowOp.Jump, 1, 0, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = new[] { 1 },
            });
            method.Blocks.Add(new LowBlock
            {
                Phis = new List<Phi>()
                {
                    new Phi(2, new[] { 0, 3 }.ToImmutableList()),
                    new Phi(3, new[] { 1, 2 }.ToImmutableList())
                },
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 2, 3, 0), // Compare #2, #3
                    new LowInstruction(LowOp.JumpIfLess, 3, 0, 0, 0), // JumpIfLess LB_3
                    new LowInstruction(LowOp.Jump, 2, 0, 0, 0), // Jump LB_2
                },
                Predecessors = new[] { 0, 2 },
                Successors = new[] { 3, 2 },
            });
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Jump, 1, 0, 0, 0) // Jump LB_1
                },
                Predecessors = new[] { 1 },
                Successors = new[] { 1 },
            });
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 4, 0, 0, 0), // Void return
                    new LowInstruction(LowOp.Return, 0, 4, 0, 0) // Return
                },
                Predecessors = new[] { 1 },
                Successors = Array.Empty<int>(),
            });

            // Act
            var (converted, map) = X64RegisterAllocator.Allocate(method);

            // Assert
            AssertDump(converted, @"
LB_0:
    LoadInt 0 0 10 -> 0
    LoadInt 0 0 11 -> 1
    Jump 0 0 0 -> 1
LB_1:
    Compare 2 3 0 -> 0
    JumpIfLess 0 0 0 -> 3
    Jump 0 0 0 -> 2
LB_2:
    Swap 3 2 0 -> 0
    Jump 0 0 0 -> 1
LB_3:
    LoadInt 0 0 0 -> 4
    Return 4 0 0 -> 0");

            // The Phi destinations must have intersecting intervals and therefore different registers,
            // while the values from the initial block should not need any copies
            Assert.That(map.Get(3).location, Is.Not.EqualTo(map.Get(4).location));
            Assert.That(map.Get(0).location, Is.EqualTo(map.Get(2).location));
            Assert.That(map.Get(1).location, Is.EqualTo(map.Get(3).location));
        }

        [TestCase(LowOp.Call)]
        [TestCase(LowOp.CallImported)]
        public void Call_instruction_reserves_registers(LowOp callOp)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Void, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.LoadInt, 2, 0, 0, 1), // Load 1 -> #2
                    new LowInstruction(LowOp.LoadInt, 3, 0, 0, 1), // Load 1 -> #3
                    new LowInstruction(LowOp.LoadInt, 4, 0, 0, 1), // Load 1 -> #4

                    new LowInstruction(callOp, 5, 0, 0, 1234), // Call - this trashes rax, rcx, rdx, r8 and r9

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Test #0
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.Test, 0, 4, 0, 0), // Test #4
                    new LowInstruction(LowOp.Return, 5, 0, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (_, allocationMap) = X64RegisterAllocator.Allocate(method);

            // No local variable should be assigned to a blocked register
            for (var i = 0; i < allocationMap.IntervalCount; i++)
            {
                var (location, localIndex) = allocationMap.Get(i);

                if (localIndex == -1)
                    continue;

                if (localIndex == 5)
                {
                    Assert.That(location.Register, Is.EqualTo(X64Register.Rax));
                    continue;
                }

                Assert.That(location.IsSet, Is.True);
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.Rax));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.Rcx));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.Rdx));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.R8));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.R9));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.R10));
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.R11));

                // ..and as a general sanity check, do not allocate the stack pointer!
                Assert.That(location.Register, Is.Not.EqualTo(X64Register.Rsp));
            }
        }

        [TestCase(LowOp.IntegerSubtract)]
        [TestCase(LowOp.ShiftLeft)]
        [TestCase(LowOp.ShiftArithmeticRight)]
        public void Noncommutative_arithmetic_destination_is_not_same_as_right(LowOp op)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1

                    new LowInstruction(op, 2, 0, 1, 0), // Subtract/Shift #0 - #1 -> #2

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Use #0
                    new LowInstruction(LowOp.Move, 3, 2, 0, 0), // Use #2
                    new LowInstruction(LowOp.Return, 0, 3, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            AssertDump(rewritten, $@"
LB_0:
    LoadInt 0 0 1 -> 0
    LoadInt 0 0 1 -> 1
    {op} 0 1 0 -> 2
    Test 0 0 0 -> 0
    Move 2 0 0 -> 3
    Return 3 0 0 -> 0");

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));
            Assert.That(allocationMap.Get(2).localIndex, Is.EqualTo(2));

            // It would be tempting to assign #1 and #2 the same register, but that
            // is not good for x64: we would have to emit "mov r1, r0; sub r1, r1" where
            // local #1 is stored in r1 but local #2 lives there up until the last instruction.
            Assert.That(allocationMap.Get(1).location.Register,
                Is.Not.EqualTo(allocationMap.Get(2).location.Register));
        }

        [TestCase(LowOp.IntegerDivide)]
        [TestCase(LowOp.IntegerModulo)]
        public void Division_reserves_rdx(LowOp op)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.LoadInt, 2, 0, 0, 1), // Load 1 -> #2

                    new LowInstruction(op, 3, 2, 2, 0), // Divide #2 / #2 -> #3

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Use #0
                    new LowInstruction(LowOp.Move, 0, 1, 0, 0), // Use #1
                    new LowInstruction(LowOp.Return, 0, 3, 0, 0)
                },
                Predecessors = Array.Empty<int>(),
                Successors = Array.Empty<int>()
            });

            var (_, allocationMap) = X64RegisterAllocator.Allocate(method);

            // RDX holds the upper part of dividend and therefore must be reserved
            for (var i = 0; i < allocationMap.IntervalCount; i++)
            {
                var (location, localIndex) = allocationMap.Get(i);
                if (localIndex >= 0)
                {
                    Assert.That(location.Register, Is.Not.EqualTo(X64Register.Rdx));
                }
            }
        }

        private static void AssertDump(LowMethod<X64Register> method, string expected)
        {
            var dumpWriter = new StringWriter();
            method.Dump(dumpWriter, false);

            Assert.That(dumpWriter.ToString().Replace("\r\n", "\n").Trim(),
                Is.EqualTo(expected.Replace("\r\n", "\n").Trim()));
        }
    }
}
