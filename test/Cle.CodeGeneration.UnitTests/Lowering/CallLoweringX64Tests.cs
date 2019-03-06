using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.Lowering
{
    internal class CallAndParamLoweringX64Tests : LoweringTestBase
    {
        [Test]
        public void Parameterless_void_call()
        {
            const string source = @"
; #0 void
; #1 void
; #2 void
BB_0:
    Call Other::Method() -> #0
    Call Other::Method() -> #1
    Return #2
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 void [?]
; #1 void [?]
; #2 void [?]
LB_0:
    Call 0 0 100 -> 0
    Call 1 0 100 -> 0
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Method_parameters_have_assigned_registers()
        {
            // TODO: Stack positions too
            const string source = @"
; #0 int32 param
; #1 int32 param
; #2 int32 param
; #3 int32 param
BB_0:
    Return #0
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 int32 [rcx]
; #1 int32 [rdx]
; #2 int32 [r8]
; #3 int32 [r9]
; #4 int32 [rax]
LB_0:
    Move 0 0 0 -> 4
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }

        [Test]
        public void Method_call_passes_parameters_and_result_on_registers()
        {
            // TODO: Stack positions too
            const string source = @"
; #0 int32
; #1 int32
; #2 int32
; #3 int32
; #4 int32
BB_0:
    Call Other(#1, #0, #2, #3) -> #4
    Return #4
";
            var method = MethodAssembler.Assemble(source, "Test::Method");
            var lowered = LoweringX64.Lower(method);

            const string expected = @"
; #0 int32 [?]
; #1 int32 [?]
; #2 int32 [?]
; #3 int32 [?]
; #4 int32 [?]
; #5 int32 [rcx]
; #6 int32 [rdx]
; #7 int32 [r8]
; #8 int32 [r9]
; #9 int32 [rax]
; #10 int32 [rax]
LB_0:
    Move 1 0 0 -> 5
    Move 0 0 0 -> 6
    Move 2 0 0 -> 7
    Move 3 0 0 -> 8
    Call 0 0 100 -> 0
    Move 9 0 0 -> 4
    Move 4 0 0 -> 10
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }
    }
}
