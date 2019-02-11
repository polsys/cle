using System;
using System.IO;
using Cle.CodeGeneration.Lir;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.Lowering
{
    public class BasicLoweringX64Tests
    {
        [Test]
        public void Constant_integer_load_and_return()
        {
            var method = new CompiledMethod("Test::Method");
            method.AddLocal(SimpleType.Int32, LocalFlags.None);
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Load, 1234, 0, 0);
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            method.Body = graphBuilder.Build();

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

        private static void AssertDump<TRegister>(LowMethod<TRegister> method, string expected)
            where TRegister : struct, Enum
        {
            var dumpWriter = new StringWriter();
            method.Dump(dumpWriter);

            Assert.That(dumpWriter.ToString().Replace("\r\n", "\n").Trim(), 
                Is.EqualTo(expected.Replace("\r\n", "\n").Trim()));
        }
    }
}
