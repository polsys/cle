using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class CallTests : X64CodeGeneratorTestBase
    {
        [Test]
        public void Parameterless_void_call()
        {
            // DoNothing();
            // return;
            const string source = @"
; #0   void
; #1   void
BB_0:
    Call Test::DoNothing() -> #0
    Return #1";

            // The stack must be aligned at 16 bytes and assigned shadow space for the callee
            // TODO: A good peephole optimizer would convert the call to a jmp (removing the stack adjustment)
            // TODO: The return value could be left uninitialized (currently, LSRA wants SSA form)
            const string expected = @"
; Test::Method
LB_0:
    sub rsp, 0x28
    call Test::DoNothing
    xor eax, eax
    add rsp, 0x28
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Parameterless_imported_call()
        {
            // DoNothing();
            // return;
            const string source = @"
; #0   void
; #1   void
BB_0:
    Call Test::DoNothing() import -> #0
    Return #1";

            const string expected = @"
; Test::Method
LB_0:
    sub rsp, 0x28
    call qword ptr [Test::DoNothing]
    xor eax, eax
    add rsp, 0x28
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }

        [Test]
        public void Two_general_register_parameters_in_call()
        {
            // DoSomething(1, true);
            // return;
            const string source = @"
; #0   void
; #1   void
; #2   int32
; #3   bool
BB_0:
    Load 1 -> #2
    Load true -> #3
    Call Test::DoSomething(#2, #3) -> #0
    Return #1";
            
            const string expected = @"
; Test::Method
LB_0:
    sub rsp, 0x28
    mov ecx, 0x1
    mov edx, 0x1
    call Test::DoSomething
    xor eax, eax
    add rsp, 0x28
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
