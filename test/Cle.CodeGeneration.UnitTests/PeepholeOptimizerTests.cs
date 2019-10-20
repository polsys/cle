using System.IO;
using Cle.CodeGeneration.Lir;
using Cle.Common.TypeSystem;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests
{
    internal class PeepholeOptimizerTests
    {
        [Test]
        public void Optimizer_handles_near_empty_method()
        {
            var method = new LowMethod<X64Register>();
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Nop, 0, 0, 0, 0)
                }
            });

            Assert.That(() => PeepholeOptimizer<X64Register>.Optimize(method), Throws.Nothing);
        }

        [Test]
        public void Load_and_unnecessary_move_are_folded()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1234), // Load 1234 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0) // Move #0 -> #1
                }
            });

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
LB_0:
    LoadInt 0 0 1234 -> 1
";
            OptimizeAndVerify(method, expected);
        }

        [TestCase(LowOp.IntegerAdd)]
        [TestCase(LowOp.IntegerSubtract)]
        [TestCase(LowOp.IntegerMultiply)]
        [TestCase(LowOp.ShiftLeft)]
        [TestCase(LowOp.ShiftArithmeticRight)]
        public void Load_right_and_arithmetic_are_folded(LowOp arithmeticOp)
        {
            // The right operand of shift has special location on x64
            var isShift = arithmeticOp == LowOp.ShiftLeft || arithmeticOp == LowOp.ShiftArithmeticRight;
            var rightLocation = isShift ? X64Register.Rdx : X64Register.Invalid;

            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, requiredLocation: X64Register.Rcx));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, requiredLocation: rightLocation));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1234), // Load 1234 -> #1
                    new LowInstruction(arithmeticOp, 2, 0, 1, 0) // #0 op #1 -> #2
                }
            });

            var expected = @$"
; #0 int32 [rcx]
; #1 int32 [{(isShift ? "rdx" : "?")}]
; #2 int32 [?]
LB_0:
    {arithmeticOp} 0 -1 1234 -> 2
";
            OptimizeAndVerify(method, expected);
        }

        [TestCase(LowOp.IntegerAdd)]
        // Does not apply to IntegerSubtract due to non-commutativity
        [TestCase(LowOp.IntegerMultiply)]
        public void Load_left_and_arithmetic_are_folded(LowOp arithmeticOp)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, requiredLocation: X64Register.Rcx));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 1234), // Load 1234 -> #1
                    new LowInstruction(arithmeticOp, 2, 1, 0, 0) // #1 op #0 -> #2
                }
            });

            var expected = @$"
; #0 int32 [rcx]
; #1 int32 [?]
; #2 int32 [?]
LB_0:
    {arithmeticOp} 0 -1 1234 -> 2
";
            OptimizeAndVerify(method, expected);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Load_and_arithmetic_are_not_folded_if_immediate_requires_8_bytes(bool immediateOnRight)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, requiredLocation: X64Register.Rcx));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 1, 0, 0, 0x1_FFFF_FFFF), // Load 1234 -> #1
                    new LowInstruction(LowOp.IntegerAdd, 2,
                        immediateOnRight ? 0 : 1, immediateOnRight ? 1 : 0, 0) // #0 + #1 -> #2
                }
            });

            OptimizeAndVerifyUnchanged(method);
        }

        [Test]
        public void Load_and_several_unnecessary_moves_are_folded()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1234), // Load 1234 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Move, 2, 1, 0, 0), // Move #1 -> #2
                    new LowInstruction(LowOp.Move, 3, 2, 0, 0) // Move #2 -> #3
                }
            });

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [?]
LB_0:
    LoadInt 0 0 1234 -> 3
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void Load_and_necessary_move_are_not_folded()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.LoadInt, 0, 0, 0, 1234), // Load 1234 -> #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Test, 0, 0, 0, 0) // Test #0 - this uses #0 so the move cannot be folded
                }
            });
            
            OptimizeAndVerifyUnchanged(method);
        }

        [Test]
        public void Unnecessary_temporary_moves_are_folded()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Move, 2, 1, 0, 0), // Move #1 -> #2
                    new LowInstruction(LowOp.Move, 3, 2, 0, 0), // Move #2 -> #3
                }
            });

            const string expected = @"
; #0 int32 [rax]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
LB_0:
    Move 0 0 0 -> 3
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void Necessary_temporary_move_is_not_folded()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Move, 2, 1, 0, 0), // Move #1 -> #2
                    new LowInstruction(LowOp.Move, 3, 2, 0, 0), // Move #2 -> #3
                    new LowInstruction(LowOp.Test, 0, 1, 0, 0), // Test #1
                }
            });

            const string expected = @"
; #0 int32 [rax]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
LB_0:
    Move 0 0 0 -> 1
    Move 1 0 0 -> 3
    Test 1 0 0 -> 0
";
            OptimizeAndVerify(method, expected);
        }

        [TestCase(LowOp.SetIfEqual)]
        [TestCase(LowOp.SetIfNotEqual)]
        [TestCase(LowOp.SetIfGreater)]
        public void SetIfX_and_move_are_folded(LowOp op)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(op, 0, 0, 0, 0), // SetIfX #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Return, 0, 1, 0, 0) // Return
                }
            });

            var expected = $@"
; #0 bool [?]
; #1 bool [rax]
LB_0:
    {op} 0 0 0 -> 1
    Return 1 0 0 -> 0
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void SetIfEqual_and_move_are_not_folded_if_original_local_is_used()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, X64Register.Rax));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.SetIfEqual, 0, 0, 0, 0), // SetIfEqual #0
                    new LowInstruction(LowOp.Move, 1, 0, 0, 0), // Move #0 -> #1
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0) // Return
                }
            });
            
            OptimizeAndVerifyUnchanged(method);
        }

        [TestCase(LowOp.SetIfEqual, "JumpIfEqual")]
        [TestCase(LowOp.SetIfLess, "JumpIfLess")]
        [TestCase(LowOp.SetIfLessOrEqual, "JumpIfLessOrEqual")]
        public void Jump_if_equal_pattern_is_folded(LowOp comparisonOp, string expectedFoldedOp)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(comparisonOp, 2, 0, 0, 0), // SetIfEqual #2
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2
                    new LowInstruction(LowOp.JumpIfNotEqual, 1, 0, 0, 0) // JumpIfNotEqual (not zero) LB_1
                }
            });

            var expected = $@"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [?]
LB_0:
    Compare 0 1 0 -> 0
    {expectedFoldedOp} 0 0 0 -> 1
";
            OptimizeAndVerify(method, expected);
        }

        [TestCase(LowOp.SetIfEqual, "JumpIfNotEqual")]
        [TestCase(LowOp.SetIfLess, "JumpIfGreaterOrEqual")]
        [TestCase(LowOp.SetIfLessOrEqual, "JumpIfGreater")]
        public void Jump_if_not_equal_pattern_is_folded(LowOp comparisonOp, string expectedFoldedOp)
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(comparisonOp, 2, 0, 0, 0), // SetIfEqual #2
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2 (negation, part 1)
                    new LowInstruction(LowOp.SetIfEqual, 3, 0, 0, 0), // SetIfZero #3 (negation, part 2)
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.JumpIfNotEqual, 1, 0, 0, 0) // JumpIfNotEqual (not zero) LB_1
                }
            });

            var expected = $@"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [?]
; #3 bool [?]
LB_0:
    Compare 0 1 0 -> 0
    {expectedFoldedOp} 0 0 0 -> 1
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void Jump_if_equal_pattern_is_partially_folded_if_result_is_used()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(LowOp.SetIfEqual, 2, 0, 0, 0), // SetIfEqual #2
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2
                    new LowInstruction(LowOp.JumpIfNotEqual, 1, 0, 0, 0), // JumpIfNotEqual (not zero) LB_1
                    new LowInstruction(LowOp.Return, 0, 0, 0, 0), // Return (#2)
                }
            });

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [rax]
LB_0:
    Compare 0 1 0 -> 0
    SetIfEqual 0 0 0 -> 2
    JumpIfEqual 0 0 0 -> 1
    Return 0 0 0 -> 0
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void Jump_if_not_equal_pattern_is_partially_folded_if_result_is_used()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, X64Register.Rax));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(LowOp.SetIfEqual, 2, 0, 0, 0), // SetIfEqual #2
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2 (negation, part 1)
                    new LowInstruction(LowOp.SetIfEqual, 3, 0, 0, 0), // SetIfZero #3 (negation, part 2)
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.JumpIfNotEqual, 1, 0, 0, 0), // JumpIfNotEqual (not zero) LB_1
                    new LowInstruction(LowOp.Return, 0, 3, 0, 0), // Return (#3)
                }
            });

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [?]
; #3 bool [rax]
LB_0:
    Compare 0 1 0 -> 0
    SetIfNotEqual 0 0 0 -> 3
    JumpIfNotEqual 0 0 0 -> 1
    Return 3 0 0 -> 0
";
            OptimizeAndVerify(method, expected);
        }

        [Test]
        public void Jump_if_not_equal_pattern_is_partially_folded_if_non_negated_result_is_used()
        {
            var method = new LowMethod<X64Register>();
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool, X64Register.Rax));
            method.Locals.Add(new LowLocal<X64Register>(SimpleType.Bool));
            method.Blocks.Add(new LowBlock
            {
                Instructions =
                {
                    new LowInstruction(LowOp.Compare, 0, 0, 1, 0), // Compare #0, #1
                    new LowInstruction(LowOp.SetIfEqual, 2, 0, 0, 0), // SetIfEqual #2
                    new LowInstruction(LowOp.Test, 0, 2, 0, 0), // Test #2 (negation, part 1)
                    new LowInstruction(LowOp.SetIfEqual, 3, 0, 0, 0), // SetIfZero #3 (negation, part 2)
                    new LowInstruction(LowOp.Test, 0, 3, 0, 0), // Test #3
                    new LowInstruction(LowOp.JumpIfNotEqual, 1, 0, 0, 0), // JumpIfNotEqual (not zero) LB_1
                    new LowInstruction(LowOp.Return, 0, 2, 0, 0), // Return #2
                }
            });

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [rax]
; #3 bool [?]
LB_0:
    Compare 0 1 0 -> 0
    SetIfEqual 0 0 0 -> 2
    JumpIfNotEqual 0 0 0 -> 1
    Return 2 0 0 -> 0
";
            OptimizeAndVerify(method, expected);
        }

        private static void OptimizeAndVerify(LowMethod<X64Register> method, string expected)
        {
            PeepholeOptimizer<X64Register>.Optimize(method);

            var dumpWriter = new StringWriter();
            method.Dump(dumpWriter, true);

            Assert.That(dumpWriter.ToString().Replace("\r\n", "\n").Trim(),
                Is.EqualTo(expected.Replace("\r\n", "\n").Trim()));
        }

        private static void OptimizeAndVerifyUnchanged(LowMethod<X64Register> method)
        {
            var expected = new StringWriter();
            method.Dump(expected, true);

            OptimizeAndVerify(method, expected.ToString());
        }
    }
}
