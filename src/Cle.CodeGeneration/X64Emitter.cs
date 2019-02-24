using System;
using System.IO;
using Cle.CodeGeneration.Lir;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// Provides methods for emitting x86-64 instructions to a stream.
    /// Executable metadata, method padding, etc. are not handled by this class.
    /// </summary>
    internal sealed class X64Emitter
    {
        [NotNull] private readonly Stream _outputStream;
        [CanBeNull] private readonly TextWriter _disassemblyWriter;
        private readonly byte[] _tempBuffer = new byte[8];

        private const string Indent = "    ";

        public X64Emitter([NotNull] Stream outputStream, [CanBeNull] TextWriter disassemblyWriter)
        {
            _outputStream = outputStream;
            _disassemblyWriter = disassemblyWriter;
        }

        /// <summary>
        /// Emits a single-byte no-op.
        /// </summary>
        public void EmitNop()
        {
            _outputStream.WriteByte(0x90);
            _disassemblyWriter?.WriteLine(Indent + "nop");
        }

        /// <summary>
        /// Emits a return instruction.
        /// </summary>
        public void EmitRet()
        {
            // Near return opcode, as 64-bit operand is used in long mode
            _outputStream.WriteByte(0xC3);
            _disassemblyWriter?.WriteLine(Indent + "ret");
        }

        public void EmitLoad(StorageLocation<X64Register> dest, ulong bytes)
        {
            if (!dest.IsRegister)
                throw new NotImplementedException("Load to stack");
            if (dest.Register >= X64Register.Xmm0)
                throw new NotImplementedException("Load to XMM register");

            // TODO: Replace "mov reg, 0" with "xor reg, reg" once arithmetic ops are implemented
            
            var (registerEncoding, needB) = GetRegisterEncoding(dest.Register);

            // If the operand can fit into a 32 bit immediate, emit a 32-bit load
            if (bytes <= uint.MaxValue)
            {
                _disassemblyWriter?.WriteLine($"{Indent}mov {Get32BitRegisterName(dest.Register)}, {bytes:X}h");

                EmitRexPrefixIfNeeded(false, false, false, needB);
                _outputStream.WriteByte((byte)(0xB8 | registerEncoding));
                Emit4ByteImmediate((uint)bytes);
            }
            else
            {
                _disassemblyWriter?.WriteLine($"{Indent}mov {GetRegisterName(dest.Register)}, {bytes:X}h");

                EmitRexPrefixIfNeeded(true, false, false, needB);
                _outputStream.WriteByte((byte)(0xB8 | registerEncoding));
                Emit8ByteImmediate(bytes);
            }
        }

        /// <summary>
        /// Emits a mov instruction from the source to the destination.
        /// The operands are considered to be full width.
        /// </summary>
        // TODO: Support specifying operand size
        public void EmitMov(StorageLocation<X64Register> dest, StorageLocation<X64Register> source)
        {
            if (!source.IsRegister || !dest.IsRegister)
                throw new NotImplementedException("Mov to/from stack");
            if (source.Register >= X64Register.Xmm0 || dest.Register >= X64Register.Xmm0)
                throw new NotImplementedException("Mov to/from XMM registers");

            // General-to-general register mov
            DisassembleRegReg("mov", dest.Register, source.Register);

            var (encodedDest, needR) = GetRegisterEncoding(dest.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(source.Register);

            EmitRexPrefixIfNeeded(true, needR, false, needB);
            _outputStream.WriteByte(0x8B);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a movzx instruction from the low 8 bits to the full register.
        /// </summary>
        public void EmitZeroExtendFromByte(StorageLocation<X64Register> srcDest)
        {
            if (!srcDest.IsRegister)
                throw new NotImplementedException("Movzx on stack");
            if (srcDest.Register >= X64Register.Xmm0)
                throw new NotImplementedException("Movzx on XMM register");

            _disassemblyWriter?.WriteLine(
                $"{Indent}movzx {GetRegisterName(srcDest.Register)}, {Get8BitRegisterName(srcDest.Register)}");

            var (encodedDest, needR) = GetRegisterEncoding(srcDest.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(srcDest.Register);

            EmitRexPrefixIfNeeded(true, needR, false, needB);
            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte(0xB6);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a cmp instruction with the two operands.
        /// The operands are considered to be full width.
        /// </summary>
        // TODO: Support specifying operand size
        public void EmitCmp(StorageLocation<X64Register> left, StorageLocation<X64Register> right)
        {
            if (!left.IsRegister || !right.IsRegister)
                throw new NotImplementedException("Cmp on stack");
            if (left.Register >= X64Register.Xmm0 || right.Register >= X64Register.Xmm0)
                throw new NotImplementedException("SIMD compare");

            // General-to-general register cmp
            DisassembleRegReg("cmp", left.Register, right.Register);

            var (encodedDest, needR) = GetRegisterEncoding(left.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(true, needR, false, needB);
            _outputStream.WriteByte(0x3B);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a test instruction with the two operands.
        /// The operands are considered to be full width.
        /// </summary>
        // TODO: Support specifying operand size
        public void EmitTest(StorageLocation<X64Register> left, StorageLocation<X64Register> right)
        {
            if (!left.IsRegister || !right.IsRegister)
                throw new NotImplementedException("Test on stack");
            if (left.Register >= X64Register.Xmm0 || right.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("SIMD test");
            
            DisassembleRegReg("test", left.Register, right.Register);

            var (encodedDest, needR) = GetRegisterEncoding(left.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(true, needR, false, needB);
            _outputStream.WriteByte(0x85);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a set byte instruction with the specified condition code.
        /// </summary>
        public void EmitSetcc(X64Condition condition, StorageLocation<X64Register> dest)
        {
            if (!dest.IsRegister)
                throw new NotImplementedException("Setcc on stack");
            if (dest.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("Setcc on XMM register");
            
            _disassemblyWriter?.WriteLine($"{Indent}set{GetConditionName(condition)} {Get8BitRegisterName(dest.Register)}");

            var (encodedReg, needB) = GetRegisterEncoding(dest.Register);

            EmitRexPrefixIfNeeded(false, false, false, needB);
            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte((byte)(0x90 | (byte)condition));
            EmitModRmForSingleRegister(encodedReg);
        }

        /// <summary>
        /// Emits an unconditional jump instruction to an undetermined position and returns a <see cref="Fixup"/>
        /// value that can be used to fix the target position once it is known.
        /// </summary>
        /// <param name="blockIndex">
        /// The destination basic block index.
        /// This will be used as a tag on the <see cref="Fixup"/>.
        /// </param>
        /// <param name="fixup">Information required for fixing the target later.</param>
        public void EmitJmpWithFixup(int blockIndex, out Fixup fixup)
        {
            // The position points to the displacement, which is 1 byte past the start of the instruction
            fixup = new Fixup(FixupType.RelativeJump, blockIndex, (int)_outputStream.Position + 1);

            _disassemblyWriter?.WriteLine(Indent + "jmp LB_" + blockIndex);

            _outputStream.WriteByte(0xE9);
            Emit4ByteImmediate((uint)blockIndex);
        }

        /// <summary>
        /// Emits a conditional jump instruction to an undetermined position and returns a <see cref="Fixup"/>
        /// value that can be used to fix the target position once it is known.
        /// </summary>
        /// <param name="condition">The condition code where the jump is executed.</param>
        /// <param name="blockIndex">
        /// The destination basic block index.
        /// This will be used as a tag on the <see cref="Fixup"/>.
        /// </param>
        /// <param name="fixup">Information required for fixing the target later.</param>
        public void EmitJccWithFixup(X64Condition condition, int blockIndex, out Fixup fixup)
        {
            // The position points to the displacement, which is 2 bytes past the start of the instruction
            fixup = new Fixup(FixupType.RelativeJump, blockIndex, (int)_outputStream.Position + 2);

            _disassemblyWriter?.WriteLine($"{Indent}j{GetConditionName(condition)} LB_{blockIndex}");

            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte((byte)(0x80 | (byte)condition));
            Emit4ByteImmediate((uint)blockIndex);
        }

        /// <summary>
        /// Applies the given fixup.
        /// </summary>
        /// <param name="fixup">The fixup returned by a previous method call.</param>
        /// <param name="newValue">
        /// This is interpreted according to the fixup type.
        /// For relative jumps, this is the address of the jump target.
        /// </param>
        public void ApplyFixup(in Fixup fixup, int newValue)
        {
            switch (fixup.Type)
            {
                case FixupType.RelativeJump:
                    _outputStream.Seek(fixup.Position, SeekOrigin.Begin);
                    Emit4ByteImmediate((uint)(newValue - fixup.Position - 4));
                    _outputStream.Seek(0, SeekOrigin.End);
                    return;
                case FixupType.Invalid:
                    throw new NotImplementedException("Unimplemented fixup type: " + fixup.Type);
            }
        }

        /// <summary>
        /// Returns the current writer position that can be used as a jump target.
        /// Writes a label for the given basic block to the disassembly stream.
        /// </summary>
        public void StartBlock(int blockIndex, out int position)
        {
            _disassemblyWriter?.WriteLine("LB_" + blockIndex + ":");
            position = (int)_outputStream.Position;
        }

        private void DisassembleRegReg(string opcode, X64Register dest, X64Register src)
        {
            _disassemblyWriter?.WriteLine($"{Indent}{opcode} {GetRegisterName(dest)}, {GetRegisterName(src)}");
        }

        private void Emit8ByteImmediate(ulong bytes)
        {
            // Just mov the bytes into the temp buffer
            unsafe
            {
                fixed (byte* buf = &_tempBuffer[0])
                {
                    var bufAsUlong = (ulong*)buf;
                    *bufAsUlong = bytes;
                }
            }

            _outputStream.Write(_tempBuffer, 0, 8);
        }

        private void Emit4ByteImmediate(uint bytes)
        {
            unsafe
            {
                fixed (byte* buf = &_tempBuffer[0])
                {
                    var bufAsUlong = (uint*)buf;
                    *bufAsUlong = bytes;
                }
            }

            _outputStream.Write(_tempBuffer, 0, 4);
        }

        private void EmitRexPrefixIfNeeded(bool w, bool r, bool x, bool b)
        {
            // Skip emitting the prefix if all the fields are zero
            if ((w | r | x | b) == false)
                return;

            byte rex = 0b_0100_0000;
            if (w)
                rex |= 0b_0000_1000;
            if (r)
                rex |= 0b_0000_0100;
            if (x)
                rex |= 0b_0000_0010;
            if (b)
                rex |= 0b_0000_0001;

            _outputStream.WriteByte(rex);
        }

        private void EmitModRmForSingleRegister(byte reg)
        {
            byte modRm = 0b_11_000_000;
            modRm |= reg;

            _outputStream.WriteByte(modRm);
        }

        private void EmitModRmForRegisterToRegister(byte dest, byte source)
        {
            byte modRm = 0b_11_000_000;
            modRm |= (byte)(dest << 3);
            modRm |= source;

            _outputStream.WriteByte(modRm);
        }

        private static (byte register, bool rex) GetRegisterEncoding(X64Register register)
        {
            switch (register)
            {
                case X64Register.Rax:
                    return (0, false);
                case X64Register.Rcx:
                    return (1, false);
                case X64Register.Rdx:
                    return (2, false);
                case X64Register.Rbx:
                    return (3, false);
                case X64Register.Rsp:
                    return (4, false);
                case X64Register.Rbp:
                    return (5, false);
                case X64Register.Rsi:
                    return (6, false);
                case X64Register.Rdi:
                    return (7, false);
                case X64Register.R8:
                    return (0, true);
                case X64Register.R9:
                    return (1, true);
                case X64Register.R10:
                    return (2, true);
                case X64Register.R11:
                    return (3, true);
                case X64Register.R12:
                    return (4, true);
                case X64Register.R13:
                    return (5, true);
                case X64Register.R14:
                    return (6, true);
                case X64Register.R15:
                    return (7, true);
                case X64Register.Xmm0:
                    return (0, false);
                case X64Register.Xmm1:
                    return (1, false);
                case X64Register.Xmm2:
                    return (2, false);
                case X64Register.Xmm3:
                    return (3, false);
                case X64Register.Xmm4:
                    return (4, false);
                case X64Register.Xmm5:
                    return (5, false);
                case X64Register.Xmm6:
                    return (6, false);
                case X64Register.Xmm7:
                    return (7, false);
                default:
                    throw new NotImplementedException("Register encoding not implemented");
            }
        }

        private static string GetRegisterName(X64Register reg)
        {
            return reg.ToString().ToLowerInvariant();
        }

        private static string Get32BitRegisterName(X64Register reg)
        {
            // This is not the most efficient code but it is only called on the disassembly path anyways
            var baseName = GetRegisterName(reg);
            if (reg > X64Register.Invalid && reg < X64Register.R8)
            {
                // rax becomes eax, etc.
                return "e" + baseName.Substring(1);
            }
            else if (reg >= X64Register.R8 && reg < X64Register.Xmm0)
            {
                // r8 becomes r8d
                return baseName + "d";
            }
            else
            {
                // Not even sensible to talk about 32-bit registers
                return baseName;
            }
        }

        private static string Get8BitRegisterName(X64Register reg)
        {
            // Same performance notes as above
            var baseName = GetRegisterName(reg);
            if (reg > X64Register.Invalid && reg < X64Register.Rsi)
            {
                // rax becomes al, etc.
                return baseName.Substring(1, 1) + "l";
            }
            else if (reg >= X64Register.Rsi && reg < X64Register.R8)
            {
                // rsi becomes sil
                return baseName.Substring(1) + "l";
            }
            else if (reg >= X64Register.R8 && reg < X64Register.Xmm0)
            {
                // r8 becomes r8b
                return baseName + "b";
            }
            else
            {
                // Not even sensible to talk about 8-bit registers
                return baseName;
            }
        }

        private static string GetConditionName(X64Condition condition)
        {
            switch (condition)
            {
                case X64Condition.Overflow:
                    return "o";
                case X64Condition.Equal:
                    return "e";
                case X64Condition.NotEqual:
                    return "ne";
                default:
                    return "??";
            }
        }
    }
}
