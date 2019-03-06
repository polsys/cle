using System.IO;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class LoggingTests : X64CodeGeneratorTestBase
    {
        [Test]
        public void Debug_log_contains_method_and_register_dump()
        {
            const string source = @"
; #0   int32 param
BB_0:
    Return #0";

            using (var dumpWriter = new StringWriter())
            {
                var codeGen = new WindowsX64CodeGenerator(Stream.Null, null);
                codeGen.EmitMethod(MethodAssembler.Assemble(source, "Test::Method"), 0, false, dumpWriter);

                var dumpString = dumpWriter.ToString();

                // Dumped LIR
                Assert.That(dumpString, Contains.Substring("Return 0 0 0 -> 0"));

                // Locals
                Assert.That(dumpString, Contains.Substring("; #0 int32 [rcx]"));
                Assert.That(dumpString, Contains.Substring("; #1 int32 [rax]"));
            }
        }
    }
}
