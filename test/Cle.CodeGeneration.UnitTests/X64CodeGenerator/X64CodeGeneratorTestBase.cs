using System.IO;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.X64CodeGenerator
{
    public class X64CodeGeneratorTestBase
    {
        protected static void EmitAndAssertDisassembly(string source, string expected)
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
