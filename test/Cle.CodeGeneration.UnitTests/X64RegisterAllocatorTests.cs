using System.Collections.Immutable;
using Cle.CodeGeneration.Lir;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests
{
    internal class X64RegisterAllocatorTests
    {
        [Test]
        public void Intersecting_variables_in_single_block_have_separate_registers()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                }
            });

            X64RegisterAllocator.Allocate(method);

            Assert.That(method.Locals[0].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.IsSet, Is.True);
            Assert.That(method.Locals[0].Location, Is.Not.EqualTo(method.Locals[1].Location));
        }

        [Test]
        public void Non_intersecting_variables_in_single_block_use_same_register()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                }
            });

            X64RegisterAllocator.Allocate(method);

            Assert.That(method.Locals[0].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.IsSet, Is.True);
            Assert.That(method.Locals[0].Location, Is.EqualTo(method.Locals[1].Location));
        }

        [TestCase(X64Register.Rax)] // Allocated by default for #0
        [TestCase(X64Register.R14)] // Never allocated in this method without a requirement
        public void Register_requirement_is_respected(X64Register required)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool)
                { Location = new StorageLocation<X64Register>(required) });
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Compare, 0, 1, 0, 0), // Compare #1, #0
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                }
            });

            X64RegisterAllocator.Allocate(method);

            Assert.That(method.Locals[0].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.Register, Is.EqualTo(required));
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

            X64RegisterAllocator.Allocate(method);

            Assert.That(method.Locals[0].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.IsSet, Is.True);
            Assert.That(method.Locals[2].Location.IsSet, Is.True);
            Assert.That(method.Locals[0].Location, Is.EqualTo(method.Locals[2].Location));
            Assert.That(method.Locals[0].Location, Is.Not.EqualTo(method.Locals[1].Location));
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

            X64RegisterAllocator.Allocate(method);

            Assert.That(method.Locals[0].Location.IsSet, Is.True);
            Assert.That(method.Locals[1].Location.IsSet, Is.True);
            Assert.That(method.Locals[2].Location.IsSet, Is.True);
            Assert.That(method.Locals[3].Location.IsSet, Is.True);
            Assert.That(method.Locals[4].Location.IsSet, Is.True);

            Assert.That(method.Locals[0].Location, Is.EqualTo(method.Locals[4].Location));
            Assert.That(method.Locals[1].Location, Is.EqualTo(method.Locals[4].Location));
            Assert.That(method.Locals[1].Location, Is.EqualTo(method.Locals[3].Location));

            Assert.That(method.Locals[0].Location, Is.Not.EqualTo(method.Locals[2].Location));
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
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1
                    new LowInstruction(LowOp.LoadInt, 2, 0, 0, 1), // Load 1 -> #2
                    new LowInstruction(LowOp.LoadInt, 3, 0, 0, 1), // Load 1 -> #3
                    new LowInstruction(LowOp.LoadInt, 4, 0, 0, 1), // Load 1 -> #4

                    new LowInstruction(LowOp.Call, 0, 0, 0, 1234), // Call - this trashes rax, rcx, rdx, r8 and r9

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Test #0
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.Test, 0, 4, 0, 0), // Test #4
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                }
            });

            X64RegisterAllocator.Allocate(method);

            foreach (var local in method.Locals)
            {
                Assert.That(local.Location.IsSet, Is.True);
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.Rax));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.Rcx));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.Rdx));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.R8));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.R9));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.R10));
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.R11));

                // ..and as a general sanity check, do not allocate the stack pointer!
                Assert.That(local.Location.Register, Is.Not.EqualTo(X64Register.Rsp));
            }
        }

        [Test]
        public void Subtraction_destination_is_not_same_as_right()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1), // Load 1 -> #0
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1), // Load 1 -> #1

                    new LowInstruction(LowOp.IntegerSubtract, 2, 0, 1, 0), // Subtract #0 - #1 -> #2

                    new LowInstruction(LowOp.Test, 0, 0, 0, 0), // Use #0
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Use #2
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0)
                }
            });

            X64RegisterAllocator.Allocate(method);

            // It would be tempting to assign #1 and #2 the same register, but that
            // is not good for x64: we would have to emit "mov r1, r0; sub r1, r1" where
            // local #1 is stored in r1 but local #2 lives there up until the last instruction.
            Assert.That(method.Locals[1].Location.Register, Is.Not.EqualTo(method.Locals[2].Location.Register));
        }
    }
}
