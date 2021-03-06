using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class BranchTests : X64CodeGeneratorTestBase
    {
        [TestCase("Equal", "je")]
        [TestCase("Less", "jl")]
        [TestCase("LessOrEqual", "jle")]
        public void Integer_comparison_and_branch(string comparison, string expectedConditionalJump)
        {
            // int32 a = 42;
            // if (a op 100)
            // {
            //     return false;
            // }
            // return true;
            var source = $@"
; #0   int32
; #1   int32
; #2   bool
; #3   bool
; #4   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {comparison} #0 == #1 -> #2
    BranchIf #2 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #3
    Return #3

BB_2:
    Load true -> #4
    Return #4";

            var expected = $@"
; Test::Method
LB_0:
    mov ecx, 0x0000002A
    cmp ecx, 0x64
    {expectedConditionalJump} LB_1
    jmp LB_2
LB_1:
    xor eax, eax
    ret
LB_2:
    mov eax, 0x00000001
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Equal", "jne")]
        [TestCase("Less", "jge")]
        [TestCase("LessOrEqual", "jg")]
        public void Inverted_integer_comparison_and_branch(string comparison, string expectedConditionalJump)
        {
            // int32 a = 42;
            // if (a !op 100)
            // {
            //     return false;
            // }
            // return true;
            var source = $@"
; #0   int32
; #1   int32
; #2   bool
; #3   bool
; #4   bool
; #5   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {comparison} #0 == #1 -> #2
    BitwiseNot #2 -> #3
    BranchIf #3 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #4
    Return #4

BB_2:
    Load true -> #5
    Return #5";

            var expected = $@"
; Test::Method
LB_0:
    mov ecx, 0x0000002A
    cmp ecx, 0x64
    {expectedConditionalJump} LB_1
    jmp LB_2
LB_1:
    xor eax, eax
    ret
LB_2:
    mov eax, 0x00000001
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Bool_comparison_and_branch()
        {
            // bool b = true;
            // if (b)
            // {
            //     return false;
            // }
            // return true;
            const string source = @"
; #0   bool
; #1   bool
; #2   bool
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #1
    Return #1

BB_2:
    Load true -> #2
    Return #2";

            const string expected = @"
; Test::Method
LB_0:
    mov ecx, 0x00000001
    test ecx, ecx
    jne LB_1
    jmp LB_2
LB_1:
    xor eax, eax
    ret
LB_2:
    mov eax, 0x00000001
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Inverted_bool_comparison_and_branch()
        {
            // bool b = true;
            // if (!b)
            // {
            //     return false;
            // }
            // return true;
            const string source = @"
; #0   bool
; #1   bool
; #2   bool
; #3   bool
BB_0:
    Load true -> #0
    BitwiseNot #0 -> #1
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #2
    Return #2

BB_2:
    Load true -> #3
    Return #3";

            const string expected = @"
; Test::Method
LB_0:
    mov ecx, 0x00000001
    test ecx, ecx
    je LB_1
    jmp LB_2
LB_1:
    xor eax, eax
    ret
LB_2:
    mov eax, 0x00000001
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Branch_with_a_single_backwards_phi()
        {
            // int a = 0;
            // bool b = true;
            // if (b)
            // {
            //     a = 1;
            // }
            // return a;
            const string source = @"
; #0   int32
; #1   bool
; #2   int32
; #3   int32
BB_0:
    Load 0 -> #0
    Load true -> #1
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Load 1 -> #2
    ==> BB_3

BB_2:

BB_3:
    PHI (#0, #2) -> #3
    Return #3";

            // TODO: The "jmp LB_3" should be elided since the real jump amount is zero
            //       This is not caught by the current heuristic that only compares the block indices
            const string expected = @"
; Test::Method
LB_0:
    xor ecx, ecx
    mov edx, 0x00000001
    test edx, edx
    jne LB_1
    jmp LB_2
LB_1:
    mov ecx, 0x00000001
    jmp LB_3
LB_2:
LB_3:
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
