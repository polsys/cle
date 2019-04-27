using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.Lowering
{
    internal class BasicLoweringX64Tests : LoweringTestBase
    {
        [Test]
        public void Constant_integer_load_and_return()
        {
            const string source = @"
; #0 int32
BB_0:
    Load 1234 -> #0
    Return #0
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 int32 [?]
; #1 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    Move 0 0 0 -> 1
    Return 1 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Boolean_negation()
        {
            const string source = @"
; #0 bool
; #1 bool
BB_0:
    Load false -> #0
    BitwiseNot #0 -> #1
    Return #1
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 bool [?]
; #1 bool [?]
; #2 bool [rax]
LB_0:
    LoadInt 0 0 0 -> 0
    Test 0 0 0 -> 0
    SetIfEqual 0 0 0 -> 1
    Move 1 0 0 -> 2
    Return 2 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [TestCase("Equal", "SetIfEqual")]
        [TestCase("Less", "SetIfLess")]
        [TestCase("LessOrEqual", "SetIfLessOrEqual")]
        public void Boolean_set_from_comparison(string highOp, string expectedLowOp)
        {
            var source = $@"
; #0 int32
; #1 int32
; #2 bool
BB_0:
    Load 1234 -> #0
    Load 5678 -> #1
    {highOp} #0 ?? #1 -> #2
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            var expected = $@"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [?]
; #3 bool [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    LoadInt 0 0 5678 -> 1
    Compare 0 1 0 -> 0
    {expectedLowOp} 0 0 0 -> 2
    Move 2 0 0 -> 3
    Return 3 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [TestCase("Add", "IntegerAdd")]
        [TestCase("Subtract", "IntegerSubtract")]
        [TestCase("Multiply", "IntegerMultiply")]
        [TestCase("BitwiseAnd", "BitwiseAnd")]
        [TestCase("BitwiseOr", "BitwiseOr")]
        [TestCase("BitwiseXor", "BitwiseXor")]
        public void Integer_binary_arithmetic(string highOp, string expectedLowOp)
        {
            var source = $@"
; #0 int32
; #1 int32
; #2 int32
BB_0:
    Load 1234 -> #0
    Load 5678 -> #1
    {highOp} #0 ?? #1 -> #2
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            var expected = $@"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    LoadInt 0 0 5678 -> 1
    {expectedLowOp} 0 1 0 -> 2
    Move 2 0 0 -> 3
    Return 3 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [TestCase("ArithmeticNegate", "IntegerNegate")]
        [TestCase("BitwiseNot", "BitwiseNot")]
        public void Integer_unary_arithmetic(string highOp, string expectedLowOp)
        {
            var source = $@"
; #0 int32
; #1 int32
BB_0:
    Load 1234 -> #0
    {highOp} #0 -> #1
    Return #1
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            var expected = $@"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    {expectedLowOp} 0 0 0 -> 1
    Move 1 0 0 -> 2
    Return 2 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Integer_division()
        {
            const string source = @"
; #0 int32
; #1 int32
; #2 int32
BB_0:
    Load 1234 -> #0
    Load 56 -> #1
    Divide #0 / #1 -> #2
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            // The dividend and result must be in RAX
            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
; #4 int32 [rax]
; #5 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    LoadInt 0 0 56 -> 1
    Move 0 0 0 -> 3
    IntegerDivide 3 1 0 -> 4
    Move 4 0 0 -> 2
    Move 2 0 0 -> 5
    Return 5 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Integer_modulo()
        {
            const string source = @"
; #0 int32
; #1 int32
; #2 int32
BB_0:
    Load 1234 -> #0
    Load 56 -> #1
    Modulo #0 % #1 -> #2
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            // The dividend must be in RAX and the result in RDX
            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
; #4 int32 [rdx]
; #5 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    LoadInt 0 0 56 -> 1
    Move 0 0 0 -> 3
    IntegerModulo 3 1 0 -> 4
    Move 4 0 0 -> 2
    Move 2 0 0 -> 5
    Return 5 0 0 -> 0
";
            AssertDump(lowered, expected);
        }
    }
}
