using System.IO;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class VariableTests
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

        private static void EmitAndAssertDisassembly(string source, string expected)
        {
            var sourceMethod = MethodAssembler.Assemble(source, "Test::Method");
            var disassemblyWriter = new StringWriter();
            var codeGen = new WindowsX64CodeGenerator(Stream.Null, disassemblyWriter);

            codeGen.EmitMethod(sourceMethod, 0, false);

            Assert.That(disassemblyWriter.ToString().Replace("\r\n", "\n").Trim(), 
                Is.EqualTo(expected.Replace("\r\n", "\n").Trim()));
        }
    }
}
