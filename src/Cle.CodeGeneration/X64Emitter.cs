using System.IO;
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
    }
}
