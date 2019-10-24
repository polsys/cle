using System.IO;
using Cle.CodeGeneration.Lir;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests
{
    public class X64EmitterTests
    {
        [Test]
        public void StartBlock_returns_block_offset()
        {
            var emitter = GetEmitter(out _, out var disassembly);
            emitter.StartBlock(0, out var first);
            emitter.EmitNop();
            emitter.StartBlock(1, out var second);
            
            Assert.That(disassembly.ToString().Replace("\r\n", "\n"), Is.EqualTo("LB_0:\n    nop\nLB_1:\n"));
            Assert.That(first, Is.EqualTo(0));
            Assert.That(second, Is.EqualTo(1)); // TODO: Blocks should start at 16-byte offsets for optimal cache use
        }

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
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov edx, 0x18"));
        }

        [Test]
        public void EmitLoad_emits_32_bit_load_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.R15),
                0xFFFFFFFF);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0xBF, 0xFF, 0xFF, 0xFF, 0xFF }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r15d, 0xFFFFFFFF"));
        }

        [Test]
        public void EmitLoad_emits_64_bit_load()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.Rax),
                0x3F00000000000001);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0xB8, 0x01, 0, 0, 0, 0, 0, 0, 0x3F }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rax, 0x3F00000000000001"));
        }

        [Test]
        public void EmitLoad_emits_64_bit_load_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitLoad(
                new StorageLocation<X64Register>(X64Register.R15),
                0x7FFFFFFFFFFFFFFF);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0xBF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F },
                stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r15, 0x7FFFFFFFFFFFFFFF"));
        }

        [Test]
        public void EmitMov_emits_64_bit_mov_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rbx), 8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x8B, 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rax, rbx"));
        }

        [Test]
        public void EmitMov_emits_64_bit_mov_from_basic_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.R14),
                new StorageLocation<X64Register>(X64Register.Rdx), 8);

            CollectionAssert.AreEqual(new byte[] { 0x4C, 0x8B, 0xF2 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov r14, rdx"));
        }

        [Test]
        public void EmitMov_emits_64_bit_mov_from_new_to_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.Rbx),
                new StorageLocation<X64Register>(X64Register.R9), 8);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0x8B, 0xD9 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov rbx, r9"));
        }

        [Test]
        public void EmitMov_emits_32_bit_mov_from_new_to_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitMov(
                new StorageLocation<X64Register>(X64Register.Rbx),
                new StorageLocation<X64Register>(X64Register.R9), 4);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x8B, 0xD9 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("mov ebx, r9d"));
        }

        [Test]
        public void EmitZeroExtendFromByte_extends_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitZeroExtendFromByte(
                new StorageLocation<X64Register>(X64Register.Rbx));

            CollectionAssert.AreEqual(new byte[] { 0x0F, 0xB6, 0xDB }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("movzx ebx, bl"));
        }

        [Test]
        public void EmitZeroExtendFromByte_extends_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitZeroExtendFromByte(
                new StorageLocation<X64Register>(X64Register.R10));

            CollectionAssert.AreEqual(new byte[] { 0x45, 0x0F, 0xB6, 0xD2 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("movzx r10d, r10b"));
        }

        [Test]
        public void EmitExchange_swaps_new_and_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitExchange(
                new StorageLocation<X64Register>(X64Register.R13),
                new StorageLocation<X64Register>(X64Register.Rcx));

            CollectionAssert.AreEqual(new byte[] { 0x4C, 0x87, 0xE9 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("xchg r13, rcx"));
        }

        [Test]
        public void EmitSetcc_emits_setne_to_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitSetcc(
                X64Condition.NotEqual,
                new StorageLocation<X64Register>(X64Register.Rax));

            CollectionAssert.AreEqual(new byte[] { 0x0F, 0x95, 0xC0 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("setne al"));
        }

        [Test]
        public void EmitSetcc_emits_sete_to_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitSetcc(
                X64Condition.Equal,
                new StorageLocation<X64Register>(X64Register.R9));

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x0F, 0x94, 0xC1 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sete r9b"));
        }

        [Test]
        public void EmitPush_pushes_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitPush(X64Register.Rdi);

            CollectionAssert.AreEqual(new byte[] { 0x57 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("push rdi"));
        }

        [Test]
        public void EmitPush_pushes_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitPush(X64Register.R9);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x51 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("push r9"));
        }

        [Test]
        public void EmitPop_pops_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitPop(X64Register.Rdi);

            CollectionAssert.AreEqual(new byte[] { 0x5F }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("pop rdi"));
        }

        [Test]
        public void EmitPop_pops_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitPop(X64Register.R9);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x59 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("pop r9"));
        }

        [Test]
        public void EmitCmp_emits_32_bit_compare_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmp(
                new StorageLocation<X64Register>(X64Register.Rbx),
                new StorageLocation<X64Register>(X64Register.Rsp), 4);

            CollectionAssert.AreEqual(new byte[] { 0x3B, 0xDC }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp ebx, esp"));
        }

        [Test]
        public void EmitCmp_emits_32_bit_compare_between_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmp(
                new StorageLocation<X64Register>(X64Register.R10),
                new StorageLocation<X64Register>(X64Register.R11), 4);

            CollectionAssert.AreEqual(new byte[] { 0x45, 0x3B, 0xD3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp r10d, r11d"));
        }

        [Test]
        public void EmitCmp_emits_64_bit_compare_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmp(
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rbx), 8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x3B, 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp rax, rbx"));
        }

        [Test]
        public void EmitCmp_emits_64_bit_compare_between_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmp(
                new StorageLocation<X64Register>(X64Register.R8),
                new StorageLocation<X64Register>(X64Register.R15), 8);

            CollectionAssert.AreEqual(new byte[] { 0x4D, 0x3B, 0xC7 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp r8, r15"));
        }

        [Test]
        public void EmitCmpWithImmediate_emits_64_bit_compare_with_byte_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmpWithImmediate(
                new StorageLocation<X64Register>(X64Register.Rax),
                -1,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x83, 0xF8, 0xFF }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp rax, 0xFF"));
        }

        [Test]
        public void EmitCmpWithImmediate_emits_32_bit_compare_with_dword_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitCmpWithImmediate(
                new StorageLocation<X64Register>(X64Register.Rbp),
                0x12345678,
                4);

            CollectionAssert.AreEqual(new byte[] { 0x81, 0xFD, 0x78, 0x56, 0x34, 0x12 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cmp ebp, 0x12345678"));
        }

        [Test]
        public void EmitTest_emits_32_bit_logical_compare_between_basic_and_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitTest(
                new StorageLocation<X64Register>(X64Register.R9),
                new StorageLocation<X64Register>(X64Register.Rax));

            CollectionAssert.AreEqual(new byte[] { 0x44, 0x85, 0xC8 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("test r9d, eax"));
        }

        [Test]
        public void EmitJcc_emits_conditional_jump()
        {
            var emitter = GetEmitter(out var stream, out var disassembly);
            emitter.EmitJcc(X64Condition.Equal, 16, 0x12345678);
            
            // The displacement is relative to the next instruction, therefore is 6 bytes less than the target byte.
            CollectionAssert.AreEqual(new byte[] { 0x0F, 0x84, 0x72, 0x56, 0x34, 0x12 }, stream.ToArray());

            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("je LB_16"));
        }

        [Test]
        public void EmitJcc_and_ApplyFixup_emit_conditional_jump()
        {
            var emitter = GetEmitter(out var stream, out var disassembly);
            emitter.EmitJccWithFixup(X64Condition.Equal, 16, out var fixup);

            // Initially, the jump target is set to 0
            CollectionAssert.AreEqual(new byte[] { 0x0F, 0x84, 0xFA, 0xFF, 0xFF, 0xFF }, stream.ToArray());

            // Applying the fixup replaces the displacement with a correctly computed value.
            // The displacement is relative to the next instruction, therefore is 6 bytes less than the target byte.
            emitter.ApplyFixup(fixup, 0x12345678);
            CollectionAssert.AreEqual(new byte[] { 0x0F, 0x84, 0x72, 0x56, 0x34, 0x12 }, stream.ToArray());

            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("je LB_16"));
        }

        [Test]
        public void EmitJcc_and_ApplyFixup_emit_correct_displacement()
        {
            var emitter = GetEmitter(out var stream, out _);

            // The nop instructions ensure that the offset is calculated correctly
            emitter.EmitNop();
            emitter.EmitJccWithFixup(X64Condition.Equal, 16, out var fixup);
            emitter.EmitNop();
            
            // Displacement is -7 bytes (1 for initial nop, 6 for je)
            emitter.ApplyFixup(fixup, 0);
            CollectionAssert.AreEqual(new byte[] { 0x90, 0x0F, 0x84, 0xF9, 0xFF, 0xFF, 0xFF, 0x90 }, stream.ToArray());
        }

        [Test]
        public void EmitJmp_and_ApplyFixup_emit_unconditional_jump()
        {
            var emitter = GetEmitter(out var stream, out var disassembly);
            emitter.EmitJmpWithFixup(16, out var fixup);

            // Initially, the jump target is 0
            CollectionAssert.AreEqual(new byte[] { 0xE9, 0xFB, 0xFF, 0xFF, 0xFF }, stream.ToArray());
            
            // The displacement is 5 bytes less than the target byte
            emitter.ApplyFixup(fixup, 0x12345678);
            CollectionAssert.AreEqual(new byte[] { 0xE9, 0x73, 0x56, 0x34, 0x12 }, stream.ToArray());

            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("jmp LB_16"));
        }

        [Test]
        public void EmitCall_emits_call()
        {
            GetEmitter(out var stream, out var disassembly).EmitCall(0x12345678, "Method::Name");

            // The displacement is relative to the next instruction, therefore is 5 bytes less than the target byte
            CollectionAssert.AreEqual(new byte[] { 0xE8, 0x73, 0x56, 0x34, 0x12 }, stream.ToArray());

            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("call Method::Name"));
        }

        [Test]
        public void EmitCallWithFixup_and_ApplyFixup_emit_correct_displacement()
        {
            var emitter = GetEmitter(out var stream, out _);

            // The nop instructions ensure that the offset is calculated correctly
            emitter.EmitNop();
            emitter.EmitCallWithFixup(16, "Method::Name", out var fixup);
            emitter.EmitNop();

            // Displacement is -6 bytes (1 for initial nop, 5 for call)
            emitter.ApplyFixup(fixup, 0);
            CollectionAssert.AreEqual(new byte[] { 0x90, 0xE8, 0xFA, 0xFF, 0xFF, 0xFF, 0x90 }, stream.ToArray());
        }

        [Test]
        public void EmitCallIndirectWithFixup_and_ApplyFixup_emit_indirect_call()
        {
            var emitter = GetEmitter(out var stream, out var disassembly);

            emitter.EmitCallIndirectWithFixup(17, "Method::Name", out var fixup);

            // Displacement is 4096 - 6 bytes for the call
            emitter.ApplyFixup(fixup, 0x1000);
            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x15, 0xFA, 0x0F, 0x00, 0x00 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("call qword ptr [Method::Name]"));
        }

        [Test]
        public void EmitGeneralUnaryOp_emits_32_bit_neg_of_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralUnaryOp(
                UnaryOp.Negate,
                new StorageLocation<X64Register>(X64Register.R12),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0xF7, 0xDC }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("neg r12d"));
        }

        [Test]
        public void EmitGeneralUnaryOp_emits_64_bit_not_of_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralUnaryOp(
                UnaryOp.Not,
                new StorageLocation<X64Register>(X64Register.Rcx),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0xF7, 0xD1 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("not rcx"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_add_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rbx),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x03, 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add eax, ebx"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_64_bit_add_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rbx),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x03, 0xC3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add rax, rbx"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_add_between_basic_and_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.R8),
                new StorageLocation<X64Register>(X64Register.Rsi),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x44, 0x03, 0xC6 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add r8d, esi"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_64_bit_add_between_basic_and_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.R8),
                new StorageLocation<X64Register>(X64Register.Rsi),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x4C, 0x03, 0xC6 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add r8, rsi"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_sub_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Subtract,
                new StorageLocation<X64Register>(X64Register.Rdi),
                new StorageLocation<X64Register>(X64Register.Rsi),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x2B, 0xFE }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sub edi, esi"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_imul_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.Multiply,
                new StorageLocation<X64Register>(X64Register.Rbp),
                new StorageLocation<X64Register>(X64Register.Rax),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x0F, 0xAF, 0xE8 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("imul ebp, eax"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_64_bit_and_between_new_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.BitwiseAnd,
                new StorageLocation<X64Register>(X64Register.R10),
                new StorageLocation<X64Register>(X64Register.R11),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x4D, 0x23, 0xD3 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("and r10, r11"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_or_between_basic_registers()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.BitwiseOr,
                new StorageLocation<X64Register>(X64Register.Rcx),
                new StorageLocation<X64Register>(X64Register.Rdx),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x0B, 0xCA }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("or ecx, edx"));
        }

        [Test]
        public void EmitGeneralBinaryOp_emits_32_bit_xor_of_single_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryOp(
                BinaryOp.BitwiseXor,
                new StorageLocation<X64Register>(X64Register.Rax),
                new StorageLocation<X64Register>(X64Register.Rax),
                4);

            CollectionAssert.AreEqual(new byte[] { 0x33, 0xC0 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("xor eax, eax"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_64_bit_stack_add_with_byte_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.Rsp),
                0x28,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x83, 0xC4, 0x28 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add rsp, 0x28"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_32_bit_add_with_negative_byte_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.Rax),
                -1,
                4);

            CollectionAssert.AreEqual(new byte[] { 0x83, 0xC0, 0xFF }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add eax, 0xFF"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_32_bit_add_with_dword_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Add,
                new StorageLocation<X64Register>(X64Register.R9),
                0x0DDF00D,
                4);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x81, 0xC1, 0x0D, 0xF0, 0xDD, 0x00 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("add r9d, 0x00DDF00D"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_64_bit_stack_sub_with_byte_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Subtract,
                new StorageLocation<X64Register>(X64Register.Rsp),
                0x20,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x83, 0xEC, 0x20 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sub rsp, 0x20"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_32_bit_sub_with_dword_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Subtract,
                new StorageLocation<X64Register>(X64Register.R13),
                -0x1234,
                4);

            CollectionAssert.AreEqual(new byte[] { 0x41, 0x81, 0xED, 0xCC, 0xED, 0xFF, 0xFF }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sub r13d, 0xFFFFEDCC"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_64_bit_imul_with_byte_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Multiply,
                new StorageLocation<X64Register>(X64Register.Rdx),
                0x12,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x6B, 0xD2, 0x12 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("imul rdx, 0x12"));
        }

        [Test]
        public void EmitGeneralBinaryWithImmediate_emits_64_bit_imul_with_dword_immediate()
        {
            GetEmitter(out var stream, out var disassembly).EmitGeneralBinaryWithImmediate(
                BinaryOp.Multiply,
                new StorageLocation<X64Register>(X64Register.Rdx),
                0x1234,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x69, 0xD2, 0x34, 0x12, 0x00, 0x00 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("imul rdx, 0x00001234"));
        }

        [Test]
        public void EmitShift_emits_32_bit_left_shift_of_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitShift(
                ShiftType.Left,
                new StorageLocation<X64Register>(X64Register.Rdx),
                4);

            CollectionAssert.AreEqual(new byte[] { 0xD3, 0xE2 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("shl edx, cl"));
        }

        [Test]
        public void EmitShiftWithImmediate_emits_32_bit_left_shift_of_basic_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitShiftWithImmediate(
                ShiftType.Left,
                new StorageLocation<X64Register>(X64Register.Rdx),
                7,
                4);

            CollectionAssert.AreEqual(new byte[] { 0xC1, 0xE2, 0x07 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("shl edx, 0x07"));
        }

        [Test]
        public void EmitShift_emits_64_bit_arithmetic_right_shift_of_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitShift(
                ShiftType.ArithmeticRight,
                new StorageLocation<X64Register>(X64Register.R8),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0xD3, 0xF8 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sar r8, cl"));
        }

        [Test]
        public void EmitShiftWithImmediate_emits_64_bit_arithmetic_right_shift_of_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitShiftWithImmediate(
                ShiftType.ArithmeticRight,
                new StorageLocation<X64Register>(X64Register.R8),
                17,
                8);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0xC1, 0xF8, 0x11 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("sar r8, 0x11"));
        }

        [Test]
        public void EmitSignedDivide_emits_32_bit_divide_by_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitSignedDivide(
                new StorageLocation<X64Register>(X64Register.Rbx),
                4);

            CollectionAssert.AreEqual(new byte[] { 0xF7, 0xFB }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("idiv ebx"));
        }

        [Test]
        public void EmitSignedDivide_emits_64_bit_divide_by_new_register()
        {
            GetEmitter(out var stream, out var disassembly).EmitSignedDivide(
                new StorageLocation<X64Register>(X64Register.R14),
                8);

            CollectionAssert.AreEqual(new byte[] { 0x49, 0xF7, 0xFE }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("idiv r14"));
        }

        [Test]
        public void EmitExtendRaxToRdx_emits_32_bit_sign_extension()
        {
            GetEmitter(out var stream, out var disassembly).EmitExtendRaxToRdx(4);

            CollectionAssert.AreEqual(new byte[] { 0x99 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cdq"));
        }

        [Test]
        public void EmitExtendRaxToRdx_emits_64_bit_sign_extension()
        {
            GetEmitter(out var stream, out var disassembly).EmitExtendRaxToRdx(8);

            CollectionAssert.AreEqual(new byte[] { 0x48, 0x99 }, stream.ToArray());
            Assert.That(disassembly.ToString().Trim(), Is.EqualTo("cqo"));
        }

        private static X64Emitter GetEmitter(out MemoryStream stream, out StringWriter disassembly)
        {
            stream = new MemoryStream();
            disassembly = new StringWriter();
            return new X64Emitter(stream, disassembly);
        }
    }
}
