using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class BranchTests : X64CodeGeneratorTestBase
    {
        [Test]
        public void Integer_comparison_and_branch()
        {
            // int32 a = 42;
            // if (a == 100)
            // {
            //     return false;
            // }
            // return true;
            const string source = @"
; #0   int32
; #1   int32
; #2   bool
; #3   bool
; #4   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    Equal #0 == #1 -> #2
    BranchIf #2 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #3
    Return #3

BB_2:
    Load true -> #4
    Return #4";

            const string expected = @"
; Test::Method
LB_0:
    mov eax, 2Ah
    mov ebx, 64h
    cmp rax, rbx
    je LB_1
    jmp LB_2
LB_1:
    mov eax, 0h
    ret
LB_2:
    mov eax, 1h
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
    mov eax, 1h
    test rax, rax
    jne LB_1
    jmp LB_2
LB_1:
    mov eax, 0h
    ret
LB_2:
    mov eax, 1h
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

BB_2:
    PHI (#0, #2) -> #3
    Return #3";

            const string expected = @"
; Test::Method
LB_0:
    mov eax, 0h
    mov ebx, 1h
    test rbx, rbx
    jne LB_1
    jmp LB_2
LB_1:
    mov eax, 1h
LB_2:
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
