using System.IO;
using Cle.CodeGeneration.Lir;
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

        [Test]
        public void EmitLoad_emits_32_bit_load()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.Rdx),
                0x18);

            CollectionAssert.AreEqual(new byte[] { 0xBA, 0x18, 0x00, 0x00, 0x00 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov edx, 18h"));
        }

        [Test]
        public void EmitLoad_emits_32_bit_load_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.R15),
                0xFFFFFFFF);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0xBF, 0xFF, 0xFF, 0xFF, 0xFF }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r15d, FFFFFFFFh"));
        }

        [Test]
        public void EmitLoad_emits_64_bit_load()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.Rax),
                0x3F00000000000001);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0xB8, 0x01, 0, 0, 0, 0, 0, 0, 0x3F }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rax, 3F00000000000001h"));
        }

        [Test]
        public void EmitLoad_emits_64_bit_load_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.R15),
                0x7FFFFFFFFFFFFFFF);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0xBF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F },
                stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r15, 7FFFFFFFFFFFFFFFh"));
        }

        [Test]
        public void EmitMov_emits_register_to_register_mov_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rbx));

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x8B, 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rax, rbx"));
        }

        [Test]
        public void EmitMov_emits_register_to_register_mov_from_basic_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.R14),
                new StorageLocation<X64Register>(X64Register.Rdx));

            CollectionAssert.AreEqual(new byte[] { 0x4C, 0x8B, 0xF2 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r14, rdx"));
        }

        [Test]
        public void EmitMov_emits_register_to_register_mov_from_new_to_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.Rbx),
                new StorageLocation<X64Register>(X64Register.R9));

            CollectionAssert.AreEqual(new byte[] { 0x49, 0x8B, 0xD9 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rbx, r9"));
        }

        private static X64Emitter GetEmitter(out MemoryStream stream, out StringWriter disassembly)
        {
            stream = new MemoryStream();
            disassembly = new StringWriter();
            return new X64Emitter(stream, disassembly);
        }
    }
}
