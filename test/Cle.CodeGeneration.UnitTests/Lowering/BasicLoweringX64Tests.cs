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
    Return 0 0 0 -> 0
";
            AssertDump(lowered, expected);
        }
    }
}
