using System;
using System.IO;
using Cle.CodeGeneration.Lir;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// Provides methods for emitting x86-64 instructions to a stream.
    /// Executable metadata, method padding, etc. are not handled by this class.
    /// </summary>
    internal sealed class X64Emitter
    {
        private readonly Stream _outputStream;
        private readonly TextWriter? _disassemblyWriter;
        private readonly byte[] _tempBuffer = new byte[8];

        private const string Indent = "    ";

        public X64Emitter(Stream outputStream, TextWriter? disassemblyWriter)
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
                _disassemblyWriter?.WriteLine($"{Indent}mov {GetRegisterName(dest.Register, 4)}, 0x{bytes:X}");

                EmitRexPrefixIfNeeded(false, false, false, needB);
                _outputStream.WriteByte((byte)(0xB8 | registerEncoding));
                Emit4ByteImmediate((uint)bytes);
            }
            else
            {
                _disassemblyWriter?.WriteLine($"{Indent}mov {GetRegisterName(dest.Register, 8)}, 0x{bytes:X}");

                EmitRexPrefixIfNeeded(true, false, false, needB);
                _outputStream.WriteByte((byte)(0xB8 | registerEncoding));
                Emit8ByteImmediate(bytes);
            }
        }

        /// <summary>
        /// Emits a mov instruction from the source to the destination.
        /// The operand size is specified in bytes.
        /// </summary>
        public void EmitMov(StorageLocation<X64Register> dest, StorageLocation<X64Register> source, int operandSize)
        {
            // TODO: 8-bit ops need their own special handling, as the opcodes are different
            // TODO: 16-bit ops use the same opcode but need tests for that case
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            if (!source.IsRegister || !dest.IsRegister)
                throw new NotImplementedException("Mov to/from stack");
            if (source.Register >= X64Register.Xmm0 || dest.Register >= X64Register.Xmm0)
                throw new NotImplementedException("Mov to/from XMM registers");

            // General-to-general register mov
            DisassembleRegReg("mov", dest.Register, source.Register, operandSize);

            var (encodedDest, needR) = GetRegisterEncoding(dest.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(source.Register);

            EmitRexPrefixIfNeeded(operandSize == 8, needR, false, needB);
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
                $"{Indent}movzx {GetRegisterName(srcDest.Register, 4)}, {GetRegisterName(srcDest.Register, 1)}");

            var (encodedDest, needR) = GetRegisterEncoding(srcDest.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(srcDest.Register);

            // We don't need to use 64 bit wide operand size since the upper 32 bits are zeroed anyways
            EmitRexPrefixIfNeeded(false, needR, false, needB);
            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte(0xB6);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a full-width xchg instruction between the two registers.
        /// </summary>
        // TODO: This could also support the 32-bit encoding to potentially save a prefix
        public void EmitExchange(StorageLocation<X64Register> left, StorageLocation<X64Register> right)
        {
            if (!right.IsRegister || !left.IsRegister)
                throw new NotImplementedException("Xchg to/from stack");
            if (right.Register >= X64Register.Xmm0 || left.Register >= X64Register.Xmm0)
                throw new NotImplementedException("Xchg to/from XMM registers");

            DisassembleRegReg("xchg", left.Register, right.Register, 8);

            var (encodedLeft, needR) = GetRegisterEncoding(left.Register);
            var (encodedRight, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(true, needR, false, needB);
            _outputStream.WriteByte(0x87);
            EmitModRmForRegisterToRegister(encodedLeft, encodedRight);
        }

        /// <summary>
        /// Emits a general-purpose unary operation with the specified operand width.
        /// </summary>
        /// <param name="op">The unary operation to emit.</param>
        /// <param name="srcDest">The location used both as a source and a destination. Must not be an XMM register.</param>
        /// <param name="operandSize">The operand width in bytes.</param>
        public void EmitGeneralUnaryOp(UnaryOp op, StorageLocation<X64Register> srcDest, int operandSize)
        {
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            if (!srcDest.IsRegister)
                throw new NotImplementedException("Unary op on stack");
            if (srcDest.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("Trying to emit a general-purpose op on a SIMD register.");
            
            DisassembleSingleReg(GetGeneralUnaryName(op), srcDest.Register, operandSize);

            var (encodedReg, needB) = GetRegisterEncoding(srcDest.Register);

            EmitRexPrefixIfNeeded(operandSize == 8, false, false, needB);
            WriteGeneralUnaryOpcode(op);

            // Some of the unary ops are only distinguished by the reg field in ModRM
            switch (op)
            {
                case UnaryOp.Not:
                    _outputStream.WriteByte((byte)(0b1101_0000 | encodedReg));
                    break;
                case UnaryOp.Negate:
                    _outputStream.WriteByte((byte)(0b1101_1000 | encodedReg));
                    break;
                default:
                    throw new NotImplementedException("Unimplemented unary op");
            }
        }

        /// <summary>
        /// Emits a general-purpose binary operation with the specified operand width.
        /// </summary>
        /// <param name="op">The binary operation to emit.</param>
        /// <param name="srcDest">The left location, used both as a source and a destination. Must not be an XMM register.</param>
        /// <param name="right">The right location, used as a source. Must not be an XMM register.</param>
        /// <param name="operandSize">The operand width in bytes.</param>
        public void EmitGeneralBinaryOp(BinaryOp op, StorageLocation<X64Register> srcDest,
            StorageLocation<X64Register> right, int operandSize)
        {
            // TODO: 8-bit and 16-bit ops need their own special handling, as the opcodes are different
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            if (!srcDest.IsRegister || !right.IsRegister)
                throw new NotImplementedException("Binary op on stack");
            if (srcDest.Register >= X64Register.Xmm0 || right.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("Trying to emit a general-purpose op on a SIMD register.");
            
            DisassembleRegReg(GetGeneralBinaryName(op), srcDest.Register, right.Register, operandSize);

            var (encodedLeft, needR) = GetRegisterEncoding(srcDest.Register);
            var (encodedRight, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(operandSize == 8, needR, false, needB);
            WriteGeneralBinaryOpcode(op);
            EmitModRmForRegisterToRegister(encodedLeft, encodedRight);
        }

        /// <summary>
        /// Emits a shift instruction where the shift amount is stored in the cl register.
        /// </summary>
        /// <param name="shiftType">The shift operation to emit.</param>
        /// <param name="srcDest">The left location, used both as a source and a destination. Must not be an XMM register.</param>
        /// <param name="operandSize">The operand width in bytes.</param>
        public void EmitShift(ShiftType shiftType, StorageLocation<X64Register> srcDest, int operandSize)
        {
            // TODO: 8-bit and 16-bit ops need their own special handling, as the opcodes are different
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            if (!srcDest.IsRegister)
                throw new NotImplementedException("Shift on stack");
            if (srcDest.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("Trying to emit a general-purpose op on a SIMD register.");

            var (encodedReg, needB) = GetRegisterEncoding(srcDest.Register);
            EmitRexPrefixIfNeeded(operandSize == 8, false, false, needB);
            _outputStream.WriteByte(0xD3);

            // The shift type is encoded in the ModRM byte
            switch (shiftType)
            {
                case ShiftType.Left:
                    _outputStream.WriteByte((byte)(0b1110_0000 | encodedReg));
                    _disassemblyWriter?.WriteLine($"{Indent}shl {GetRegisterName(srcDest.Register, operandSize)}, cl");
                    break;
                case ShiftType.ArithmeticRight:
                    _outputStream.WriteByte((byte)(0b1111_1000 | encodedReg));
                    _disassemblyWriter?.WriteLine($"{Indent}sar {GetRegisterName(srcDest.Register, operandSize)}, cl");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shiftType));
            }
        }

        /// <summary>
        /// Emits a signed divide instruction with the specified width.
        /// The dividend must be stored in RDX:RAX.
        /// To sign-extend RAX to RDX, use
        /// </summary>
        /// <param name="op">The binary operation to emit.</param>
        /// <param name="right">The right location, used as a source. Must not be an XMM register.</param>
        /// <param name="operandSize">The operand width in bytes.</param>
        public void EmitSignedDivide(StorageLocation<X64Register> right, int operandSize)
        {
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            if (!right.IsRegister)
                throw new NotImplementedException("Divisor on stack");
            if (right.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("Trying to emit a general-purpose op on a SIMD register.");
            
            var (encodedRight, needB) = GetRegisterEncoding(right.Register);

            DisassembleSingleReg("idiv", right.Register, operandSize);

            EmitRexPrefixIfNeeded(operandSize == 8, false, false, needB);
            _outputStream.WriteByte(0xF7);
            _outputStream.WriteByte((byte)(0b1111_1000 | encodedRight));
        }

        /// <summary>
        /// Emits a cdq or cqo instruction.
        /// </summary>
        public void EmitExtendRaxToRdx(int operandSize)
        {
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");

            _disassemblyWriter?.WriteLine(Indent + (operandSize == 8 ? "cqo" : "cdq"));

            EmitRexPrefixIfNeeded(operandSize == 8, false, false, false);
            _outputStream.WriteByte(0x99);
        }

        /// <summary>
        /// Emits a cmp instruction with the two operands and specified operand width.
        /// </summary>
        public void EmitCmp(StorageLocation<X64Register> left, StorageLocation<X64Register> right, int operandSize)
        {
            if (operandSize != 4 && operandSize != 8)
                throw new NotImplementedException("Other operand widths");
            if (!left.IsRegister || !right.IsRegister)
                throw new NotImplementedException("Cmp on stack");
            if (left.Register >= X64Register.Xmm0 || right.Register >= X64Register.Xmm0)
                throw new NotImplementedException("SIMD compare");

            // General-to-general register cmp
            DisassembleRegReg("cmp", left.Register, right.Register, operandSize);

            var (encodedDest, needR) = GetRegisterEncoding(left.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(operandSize == 8, needR, false, needB);
            _outputStream.WriteByte(0x3B);
            EmitModRmForRegisterToRegister(encodedDest, encodedSrc);
        }

        /// <summary>
        /// Emits a test instruction with the two operands.
        /// The operands are considered to be 32 bits wide.
        /// </summary>
        // TODO: Support specifying operand size
        public void EmitTest(StorageLocation<X64Register> left, StorageLocation<X64Register> right)
        {
            if (!left.IsRegister || !right.IsRegister)
                throw new NotImplementedException("Test on stack");
            if (left.Register >= X64Register.Xmm0 || right.Register >= X64Register.Xmm0)
                throw new InvalidOperationException("SIMD test");
            
            DisassembleRegReg("test", left.Register, right.Register, 4);

            var (encodedDest, needR) = GetRegisterEncoding(left.Register);
            var (encodedSrc, needB) = GetRegisterEncoding(right.Register);

            EmitRexPrefixIfNeeded(false, needR, false, needB);
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
            
            _disassemblyWriter?.WriteLine($"{Indent}set{GetConditionName(condition)} {GetRegisterName(dest.Register, 1)}");

            var (encodedReg, needB) = GetRegisterEncoding(dest.Register);

            EmitRexPrefixIfNeeded(false, false, false, needB);
            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte((byte)(0x90 | (byte)condition));
            EmitModRmForSingleRegister(encodedReg);
        }

        /// <summary>
        /// Emits a full-width push instruction with the specified source register.
        /// </summary>
        public void EmitPush(X64Register src)
        {
            if (src >= X64Register.Xmm0)
                throw new InvalidOperationException("Push on XMM register");

            _disassemblyWriter?.WriteLine($"{Indent}push {GetRegisterName(src, 8)}");
            var (encodedReg, needB) = GetRegisterEncoding(src);

            if (needB)
            {
                // New registers need a REX.B prefix
                _outputStream.WriteByte(0x41);
            }
            _outputStream.WriteByte((byte)(0x50 | encodedReg));
        }

        /// <summary>
        /// Emits a full-width pop instruction with the specified destination register.
        /// </summary>
        public void EmitPop(X64Register dest)
        {
            if (dest >= X64Register.Xmm0)
                throw new InvalidOperationException("Pop on XMM register");

            _disassemblyWriter?.WriteLine($"{Indent}pop {GetRegisterName(dest, 8)}");
            var (encodedReg, needB) = GetRegisterEncoding(dest);

            // The opcode is encoded exactly as push, except for the 0x8 bit
            if (needB)
            {
                _outputStream.WriteByte(0x41);
            }
            _outputStream.WriteByte((byte)(0x58 | encodedReg));
        }

        /// <summary>
        /// Emits an unconditional jump instruction to the specified position.
        /// </summary>
        /// <param name="blockIndex">The destination basic block index, used for disassembly.</param>
        /// <param name="target">The jump target position. For blocks, this is received from <see cref="StartBlock"/>.</param>
        public void EmitJmp(int blockIndex, int target)
        {
            _disassemblyWriter?.WriteLine(Indent + "jmp LB_" + blockIndex);

            _outputStream.WriteByte(0xE9);
            Emit4ByteImmediate((uint)(target - _outputStream.Position - 4));
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
            EmitJmp(blockIndex, 0);
        }

        /// <summary>
        /// Emits a conditional jump instruction to the specified position.
        /// </summary>
        /// <param name="condition">The condition code where the jump is executed.</param>
        /// <param name="blockIndex">The destination basic block index, used for disassembly.</param>
        /// <param name="target">The jump target position. For blocks, this is received from <see cref="StartBlock"/>.</param>
        public void EmitJcc(X64Condition condition, int blockIndex, int target)
        {
            _disassemblyWriter?.WriteLine($"{Indent}j{GetConditionName(condition)} LB_{blockIndex}");

            _outputStream.WriteByte(0x0F);
            _outputStream.WriteByte((byte)(0x80 | (byte)condition));
            Emit4ByteImmediate((uint)(target - _outputStream.Position - 4));
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
            EmitJcc(condition, blockIndex, 0);
        }

        /// <summary>
        /// Emits a call to the specified position.
        /// </summary>
        /// <param name="target">The target byte where the execution will be transferred to.</param>
        /// <param name="methodName">The target name for disassembly.</param>
        public void EmitCall(int target, string methodName)
        {
            _disassemblyWriter?.WriteLine($"{Indent}call {methodName}");

            _outputStream.WriteByte(0xE8);
            Emit4ByteImmediate((uint)(target - _outputStream.Position - 4));
        }

        /// <summary>
        /// Emits a call to an unspecified target and returns a <see cref="Fixup"/> that can be used to set the target.
        /// </summary>
        /// <param name="targetIndex">Method index to call. This will be used as the fixup tag.</param>
        /// <param name="methodName">The target name for disassembly.</param>
        /// <param name="fixup">Information required for fixing the target later.</param>
        public void EmitCallWithFixup(int targetIndex, string methodName, out Fixup fixup)
        {
            // The call opcode is a single byte
            fixup = new Fixup(FixupType.RelativeJump, targetIndex, (int)_outputStream.Position + 1);
            EmitCall(0, methodName);
        }

        /// <summary>
        /// Emits an indirect call via an unspecified pointer and returns a <see cref="Fixup"/>
        /// that can be used to set the target.
        /// </summary>
        /// <param name="targetIndex">Method index to call. This will be used as the fixup tag.</param>
        /// <param name="methodName">The target name for disassembly.</param>
        /// <param name="fixup">Information required for fixing the target later.</param>
        public void EmitCallIndirectWithFixup(int targetIndex, string methodName, out Fixup fixup)
        {
            _disassemblyWriter?.WriteLine($"{Indent}call qword ptr [{methodName}]");

            // Emit the indirect call opcode followed by ModRM
            _outputStream.WriteByte(0xFF);
            _outputStream.WriteByte(0x15);

            // Then emit four bytes for the eventual pointer
            fixup = new Fixup(FixupType.RelativeJump, targetIndex, (int)_outputStream.Position);
            Emit4ByteImmediate(0);
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

        private void DisassembleSingleReg(string opcode, X64Register reg, int operandSize)
        {
            _disassemblyWriter?.WriteLine($"{Indent}{opcode} {GetRegisterName(reg, operandSize)}");
        }

        private void DisassembleRegReg(string opcode, X64Register left, X64Register right, int operandSize)
        {
            if (_disassemblyWriter is null)
                return;

            var leftName = GetRegisterName(left, operandSize);
            var rightName = GetRegisterName(right, operandSize);

            _disassemblyWriter.WriteLine($"{Indent}{opcode} {leftName}, {rightName}");
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

        private static string GetRegisterName(X64Register reg, int sizeInBytes)
        {
            switch (sizeInBytes)
            {
                case 1:
                    return Get8BitRegisterName(reg);
                case 4:
                    return Get32BitRegisterName(reg);
                default:
                    // Even though this is not correct for invalid values of operand size
                    return reg.ToString().ToLowerInvariant();
            }
        }

        private static string Get32BitRegisterName(X64Register reg)
        {
            // This is not the most efficient code but it is only called on the disassembly path anyways
            var baseName = reg.ToString().ToLowerInvariant();
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
            var baseName = reg.ToString().ToLowerInvariant();
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
                case X64Condition.Less:
                    return "l";
                case X64Condition.GreaterOrEqual:
                    return "ge";
                case X64Condition.LessOrEqual:
                    return "le";
                case X64Condition.Greater:
                    return "g";
                default:
                    return "??";
            }
        }

        private static string GetGeneralUnaryName(UnaryOp op)
        {
            switch (op)
            {
                case UnaryOp.Not:
                    return "not";
                case UnaryOp.Negate:
                    return "neg";
                default:
                    return "???";
            }
        }

        private static string GetGeneralBinaryName(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Add:
                    return "add";
                case BinaryOp.Subtract:
                    return "sub";
                case BinaryOp.Multiply:
                    return "imul";
                case BinaryOp.BitwiseAnd:
                    return "and";
                case BinaryOp.BitwiseOr:
                    return "or";
                case BinaryOp.BitwiseXor:
                    return "xor";
                default:
                    return "???";
            }
        }

        private void WriteGeneralUnaryOpcode(UnaryOp op)
        {
            switch (op)
            {
                case UnaryOp.Not:
                case UnaryOp.Negate:
                    _outputStream.WriteByte(0xF7);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op.ToString());
            }
        }

        private void WriteGeneralBinaryOpcode(BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.Add:
                    _outputStream.WriteByte(0x03);
                    break;
                case BinaryOp.Subtract:
                    _outputStream.WriteByte(0x2B);
                    break;
                case BinaryOp.Multiply:
                    _outputStream.WriteByte(0x0F);
                    _outputStream.WriteByte(0xAF);
                    break;
                case BinaryOp.BitwiseAnd:
                    _outputStream.WriteByte(0x23);
                    break;
                case BinaryOp.BitwiseOr:
                    _outputStream.WriteByte(0x0B);
                    break;
                case BinaryOp.BitwiseXor:
                    _outputStream.WriteByte(0x33);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op.ToString());
            }
        }
    }
}
