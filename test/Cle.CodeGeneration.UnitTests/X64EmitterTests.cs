using System.IO;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests
{
    public class X64EmitterTests
    {
        [Test]
        public void EmitNop_emits_single_byte_nop()
        {
            GetEmitter(out var stream, out var disassembly).EmitNop();

            CollectionAssert.AreEqual(new byte[] { 0x90 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("nop"));
        }

        [Test]
        public void EmitRet_emits_single_byte_return()
        {
            GetEmitter(out var stream, out var disassembly).EmitRet();

            CollectionAssert.AreEqual(new byte[] { 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("ret"));
        }

        private static X64Emitter GetEmitter(out MemoryStream stream, out StringWriter disassembly)
        {
            stream = new MemoryStream();
            disassembly = new StringWriter();
            return new X64Emitter(stream, disassembly);
        }
    }
}
