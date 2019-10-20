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
    mov eax, 0x2A
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
    mov ecx, 0x2A
    mov edx, 0x64
    cmp ecx, edx
    {expectedLowOp} al
    movzx eax, al
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
    mov ecx, 0x2A
    mov edx, 0x64
    cmp ecx, edx
    {expectedLowOp} al
    movzx eax, al
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Bool_arithmetic_and_comparison()
        {
            // return 42;
            const string source = @"
; #0   bool
; #1   bool
; #2   bool
; #3   bool
; #4   bool
BB_0:
    Load true -> #0
    Load true -> #1
    BitwiseAnd #0 & #1 -> #2
    Load false -> #3
    Equal #2 == #3 -> #4
    Return #4
";
            const string expected = @"
; Test::Method
LB_0:
    mov ecx, 0x1
    mov edx, 0x1
    and ecx, edx
    xor edx, edx
    cmp ecx, edx
    sete al
    movzx eax, al
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Add", "add")]
        [TestCase("Multiply", "imul")]
        [TestCase("BitwiseAnd", "and")]
        [TestCase("BitwiseOr", "or")]
        [TestCase("BitwiseXor", "xor")]
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
    mov ecx, 0x2A
    mov edx, 0x64
    mov eax, ecx
    {expectedAsmOp} eax, edx
    {expectedAsmOp} eax, ecx
    {expectedAsmOp} ecx, eax
    {expectedAsmOp} ecx, edx
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Add", "add")]
        [TestCase("Subtract", "sub")]
        [TestCase("Multiply", "imul")]
        [TestCase("ShiftLeft", "shl")]
        [TestCase("ShiftRight", "sar")]
        public void Basic_int32_arithmetic_with_immediate_right(string highOp, string expectedAsmOp)
        {
            // private int32 F(int32 value) { return value {op} 7; }
            var source = $@"
; #0   int32 param
; #1   int32
; #2   int32
BB_0:
    Load 7 -> #1
    {highOp} #0 ? #1 -> #2
    Return #2
";
            var expected = $@"
; Test::Method
LB_0:
    {expectedAsmOp} ecx, 0x7
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("Add", "add")]
        [TestCase("Multiply", "imul")]
        public void Basic_int32_arithmetic_with_immediate_left(string highOp, string expectedAsmOp)
        {
            // private int32 F(int32 value) { return 7 {op} value; }
            var source = $@"
; #0   int32 param
; #1   int32
; #2   int32
BB_0:
    Load 7 -> #1
    {highOp} #1 ? #0 -> #2
    Return #2
";
            var expected = $@"
; Test::Method
LB_0:
    {expectedAsmOp} ecx, 0x7
    mov eax, ecx
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
    mov ecx, 0x2A
    mov edx, 0x64
    mov eax, ecx
    {expectedAsmOp} eax, edx
    mov r8d, ecx
    {expectedAsmOp} r8d, eax
    {expectedAsmOp} ecx, r8d
    {expectedAsmOp} ecx, edx
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("ArithmeticNegate", "neg")]
        [TestCase("BitwiseNot", "not")]
        public void Basic_unary_int32_arithmetic(string highOp, string expectedAsmOp)
        {
            // int32 a = 42;
            // int32 b = -a; // Destination different from source
            // int32 c = -a; // Destination can be same as source
            // return b + c; // Use both operation results
            var source = $@"
; #0   int32
; #1   int32
; #2   int32
; #3   int32
BB_0:
    Load 43 -> #0
    {highOp} #0 -> #1
    {highOp} #0 -> #2
    Add #1 + #2 -> #3
    Return #3
";
            var expected = $@"
; Test::Method
LB_0:
    mov ecx, 0x2B
    mov edx, ecx
    {expectedAsmOp} edx
    {expectedAsmOp} ecx
    add ecx, edx
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Signed_integer_division()
        {
            const string source = @"
; #0   int32
; #1   int32
; #2   int32
BB_0:
    Load 42 -> #0
    Load 10 -> #1
    Divide #0 / #1 -> #2
    Return #2
";

            // The x64 signed division instruction requires the dividend/destination to be in edx:eax
            // The cdq instruction sign-extends eax to edx
            const string expected = @"
; Test::Method
LB_0:
    mov ecx, 0x2A
    mov r8d, 0xA
    mov eax, ecx
    cdq
    idiv r8d
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Signed_integer_modulo()
        {
            const string source = @"
; #0   int32
; #1   int32
; #2   int32
BB_0:
    Load 42 -> #0
    Load 10 -> #1
    Modulo #0 % #1 -> #2
    Return #2
";

            // As above, but the division remainder is stored in edx
            const string expected = @"
; Test::Method
LB_0:
    mov ecx, 0x2A
    mov r8d, 0xA
    mov eax, ecx
    cdq
    idiv r8d
    mov eax, edx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [TestCase("ShiftLeft", "shl")]
        [TestCase("ShiftRight", "sar")]
        // TODO: Tests for unsigned shifts
        public void Integer_shifts(string highOp, string expectedAsmOp)
        {
            // int32 a = 42;
            // int32 s = 17;
            // int32 b = a << s; // Destination different from source
            // int32 c = a << s; // Destination can be same as source
            // return b + c; // Use both operation results
            var source = $@"
; #0   int32
; #1   int32
; #2   int32
; #3   int32
; #4   int32
BB_0:
    Load 43 -> #0
    Load 17 -> #1
    {highOp} #0 ?? #1 -> #2
    {highOp} #0 ?? #1 -> #3
    Add #2 + #3 -> #4
    Return #4
";

            // There is just so much wrong with the code quality here, I don't know where to start.
            //   - The part in the end where ecx is used should be handled with register hints.
            //   - The cl register should not need to be reassigned.
            var expected = $@"
; Test::Method
LB_0:
    mov eax, 0x2B
    mov edx, 0x11
    mov ecx, edx
    mov r8d, eax
    {expectedAsmOp} r8d, cl
    mov ecx, edx
    mov edx, eax
    {expectedAsmOp} edx, cl
    mov ecx, r8d
    add ecx, edx
    mov eax, ecx
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
