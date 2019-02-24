using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.Lowering
{
    internal class BranchLoweringX64Tests : LoweringTestBase
    {
        [Test]
        public void Integer_compare_and_branch()
        {
            const string source = @"
; #0 int32
; #1 int32
; #2 bool
; #3 int32
; #4 int32
BB_0:
    Load 1234 -> #0
    Load 5678 -> #1
    Equal #0 == #1 -> #2
    BranchIf #2 ==> BB_1
    ==> BB_2
BB_1:
    Load 1 -> #3
    Return #3
BB_2:
    Load 2 -> #4
    Return #4
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 bool [?]
; #3 int32 [?]
; #4 int32 [?]
; #5 int32 [rax]
; #6 int32 [rax]
LB_0:
    LoadInt 0 0 1234 -> 0
    LoadInt 0 0 5678 -> 1
    Compare 0 1 0 -> 0
    SetIfEqual 0 0 0 -> 2
    Test 2 0 0 -> 0
    JumpIfNotEqual 0 0 0 -> 1
    Jump 0 0 0 -> 2
LB_1:
    LoadInt 0 0 1 -> 3
    Move 3 0 0 -> 5
    Return 0 0 0 -> 0
LB_2:
    LoadInt 0 0 2 -> 4
    Move 4 0 0 -> 6
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Boolean_compare_and_branch()
        {
            const string source = @"
; #0 bool
; #1 int32
; #2 int32
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2
BB_1:
    Load 1 -> #1
    Return #1
BB_2:
    Load 2 -> #2
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 bool [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [rax]
; #4 int32 [rax]
LB_0:
    LoadInt 0 0 1 -> 0
    Test 0 0 0 -> 0
    JumpIfNotEqual 0 0 0 -> 1
    Jump 0 0 0 -> 2
LB_1:
    LoadInt 0 0 1 -> 1
    Move 1 0 0 -> 3
    Return 0 0 0 -> 0
LB_2:
    LoadInt 0 0 2 -> 2
    Move 2 0 0 -> 4
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }
    }
}
