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
        /// Writes a label for the given basic block to the disassembly stream.
        /// </summary>
        public void WriteBlockLabel(int blockIndex)
        {
            _disassemblyWriter?.WriteLine("LB_" + blockIndex + ":");
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
    }
}
