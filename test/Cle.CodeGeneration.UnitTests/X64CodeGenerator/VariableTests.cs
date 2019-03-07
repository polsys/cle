using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class VariableTests : X64CodeGeneratorTestBase
    {
        [Test]
        public void Constant_integer_load_and_return()
        {
            // return 42;
            const string source = @"
; #0   int32
BB_0:
    Load 42 -> #0
    Return #0
";
            const string expected = @"
; Test::Method
LB_0:
    mov eax, 2Ah
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Equal", "sete")]
        [TestCase("Less", "setl")]
        [TestCase("LessOrEqual", "setle")]
        public void Bool_assignment_via_comparison(string highOp, string expectedLowOp)
        {
            // int32 a = 42;
            // int32 b = 100;
            // return a op b;
            var source = $@"
; #0   int32
; #1   int32
; #2   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {highOp} #0 ?? #1 -> #2
    Return #2
";
            var expected = $@"
; Test::Method
LB_0:
    mov eax, 2Ah
    mov ecx, 64h
    cmp rax, rcx
    {expectedLowOp} al
    movzx rax, al
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Equal", "setne")]
        [TestCase("Less", "setge")]
        [TestCase("LessOrEqual", "setg")]
        public void Bool_assignment_via_inverted_comparison(string highOp, string expectedLowOp)
        {
            // int32 a = 42;
            // int32 b = 100;
            // return a op b;
            var source = $@"
; #0   int32
; #1   int32
; #2   bool
; #3   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {highOp} #0 ?? #1 -> #2
    BitwiseNot #2 -> #3
    Return #3
";
            var expected = $@"
; Test::Method
LB_0:
    mov eax, 2Ah
    mov ecx, 64h
    cmp rax, rcx
    {expectedLowOp} al
    movzx rax, al
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
        
        [TestCase("Add", "add")]
        public void Basic_commutative_int32_arithmetic(string highOp, string expectedAsmOp)
        {
            // int32 a = 42;
            // int32 b = 100;
            // int32 c = a + b; // Destination different from either source
            // int32 d = a + c; // Destination can be same as right source
            // return a + d + b; // Destination can be same as left source
            var source = $@"
; #0   int32
; #1   int32
; #2   int32
; #3   int32
; #4   int32
; #5   int32
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {highOp} #0 ? #1 -> #2
    {highOp} #0 ? #2 -> #3
    {highOp} #0 ? #3 -> #4
    {highOp} #4 ? #1 -> #5
    Return #5
";
            // Since we are dealing with 32-bit operands, the registers should be 32-bit as well
            var expected = $@"
; Test::Method
LB_0:
    mov eax, 2Ah
    mov ecx, 64h
    mov edx, eax
    {expectedAsmOp} edx, ecx
    {expectedAsmOp} edx, eax
    {expectedAsmOp} eax, edx
    {expectedAsmOp} eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
        
        [TestCase("Subtract", "sub")]
        public void Basic_non_commutative_int32_arithmetic(string highOp, string expectedAsmOp)
        {
            // int32 a = 42;
            // int32 b = 100;
            // int32 c = a - b;
            // int32 d = a - c; // Cannot use the right operand as destination!
            // return a - d - b;
            var source = $@"
; #0   int32
; #1   int32
; #2   int32
; #3   int32
; #4   int32
; #5   int32
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    {highOp} #0 ? #1 -> #2
    {highOp} #0 ? #2 -> #3
    {highOp} #0 ? #3 -> #4
    {highOp} #4 ? #1 -> #5
    Return #5
";
            // Since we are dealing with 32-bit operands, the registers should be 32-bit as well
            var expected = $@"
; Test::Method
LB_0:
    mov eax, 2Ah
    mov ecx, 64h
    mov edx, eax
    {expectedAsmOp} edx, ecx
    mov r8d, eax
    {expectedAsmOp} r8d, edx
    {expectedAsmOp} eax, r8d
    {expectedAsmOp} eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
