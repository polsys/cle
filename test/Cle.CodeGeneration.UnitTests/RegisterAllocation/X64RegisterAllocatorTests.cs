using System.Collections.Immutable;
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
                    new LowInstruction(LowOp.Return, 2, 0, 0, 0)
                }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

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
                }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

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
                }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

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
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.IntegerAdd, 2, 0, 1, 0), // Add #0, #1 -> #2
                    new LowInstruction(LowOp.Jump, 0, 0, 0, 0)
                },
                Phis = new[] { new Phi(0, ImmutableList<int>.Empty.Add(2)) }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            Assert.That(allocationMap.Get(0).localIndex, Is.EqualTo(0));
            Assert.That(allocationMap.Get(1).localIndex, Is.EqualTo(1));

            Assert.That(allocationMap.Get(0).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(1).location.IsSet, Is.True);
            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(2).location));
            Assert.That(allocationMap.Get(0).location, Is.Not.EqualTo(allocationMap.Get(1).location));
        }

        [Test]
        public void Phi_referencing_another_phi()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #2
                    new LowInstruction(LowOp.IntegerAdd, 3, 1, 2, 0), // Add #1, #2 -> #3
                    new LowInstruction(LowOp.Jump, 0, 0, 0, 0)
                },
                Phis = new[] { new Phi(1, ImmutableList<int>.Empty.Add(3)) }
            });
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                },
                Phis = new[] { new Phi(4, ImmutableList<int>.Empty.Add(0).Add(1)) }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            for (var i = 0; i < 5; i++)
            {
                Assert.That(allocationMap.Get(i).localIndex, Is.EqualTo(i));
                Assert.That(allocationMap.Get(i).location.IsSet, Is.True);
            }

            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(1).location));
            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(3).location));
            Assert.That(allocationMap.Get(0).location, Is.EqualTo(allocationMap.Get(4).location));

            Assert.That(allocationMap.Get(0).location, Is.Not.EqualTo(allocationMap.Get(2).location));
        }

        [Test]
        public void Call_instruction_reserves_registers()
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

                    new LowInstruction(LowOp.Call, 5, 0, 0, 1234), // Call - this trashes rax, rcx, rdx, r8 and r9

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Test #0
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.Test, 0, 4, 0, 0), // Test #4
                    new LowInstruction(LowOp.Return, 5, 0, 0, 0)
                }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

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

        [Test]
        public void Subtraction_destination_is_not_same_as_right()
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

                    new LowInstruction(LowOp.IntegerSubtract, 2, 0, 1, 0), // Subtract #0 - #1 -> #2

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Use #0
                    new LowInstruction(LowOp.Move, 3, 2, 0, 0), // Use #2
                    new LowInstruction(LowOp.Return, 3, 0, 0, 0)
                }
            });

            var (rewritten, allocationMap) = X64RegisterAllocator.Allocate(method);

            // It would be tempting to assign #1 and #2 the same register, but that
            // is not good for x64: we would have to emit "mov r1, r0; sub r1, r1" where
            // local #1 is stored in r1 but local #2 lives there up until the last instruction.
            Assert.That(allocationMap.Get(1).location.Register,
                Is.Not.EqualTo(allocationMap.Get(2).location.Register));
        }
    }
}
