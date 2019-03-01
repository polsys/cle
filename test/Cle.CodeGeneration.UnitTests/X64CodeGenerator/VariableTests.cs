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
    mov ebx, 64h
    cmp rax, rbx
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
    mov ebx, 64h
    cmp rax, rbx
    {expectedLowOp} al
    movzx rax, al
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
