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

            // TODO: A good peephole optimizer would convert the call to a jmp
            // TODO: The return value could be left uninitialized (currently, LSRA wants SSA form)
            const string expected = @"
; Test::Method
LB_0:
    call Test::DoNothing
    mov eax, 0h
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
            
            // TODO: There should be no register shuffling (LSRA should have a register hint)
            const string expected = @"
; Test::Method
LB_0:
    mov eax, 1h
    mov edx, 1h
    mov ecx, eax
    call Test::DoSomething
    mov eax, 0h
    ret
";
            EmitAndAssertDisassembly(source, expected);
        }
    }
}
