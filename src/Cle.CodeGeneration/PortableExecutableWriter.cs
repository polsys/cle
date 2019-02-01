using System;
using System.IO;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// This class handles the creation and emission of Portable Executable (.exe, .dll) metadata.
    /// </summary>
    public sealed class PortableExecutableWriter
    {
        /// <summary>
        /// Gets the instruction emitter associated with this PE file.
        /// </summary>
        [NotNull]
        public X64Emitter Emitter { get; }

        [NotNull] private readonly Stream _exeStream;
        private int _entryPointOffset;

        private const int FileAlignment = 0x0200; // 512 bytes, the minimum allowed
        private const int BaseOfCodeAddress = 0x00001000; // The default
        private const int PeHeaderSize = 0x0400; // 1024 bytes, a safe bet

        /// <summary>
        /// Creates a PE writer instance with the specified output stream and optional disassembly stream.
        /// </summary>
        /// <param name="outputStream">
        /// Stream for the native code output.
        /// The stream must be writable, seekable and initially empty.
        /// The object lifetime is managed by the caller.
        /// </param>
        /// <param name="disassemblyWriter">
        /// Optional text writer for method disassembly.
        /// </param>
        public PortableExecutableWriter([NotNull] Stream outputStream, [CanBeNull] TextWriter disassemblyWriter)
        {
            Emitter = new X64Emitter(outputStream, disassemblyWriter);
            _exeStream = outputStream;

            // TODO: Write an actual skeleton header
            for (var i = 0; i < 1024 / 16; i++)
            {
                _exeStream.Write(_nullPadding, 0, 16);
            }
        }

        /// <summary>
        /// Prepares the emitter for a new method, by inserting appropriate padding and
        /// recording the mapping between method index and address.
        /// </summary>
        /// <param name="methodIndex">The compiler internal method index.</param>
        public void StartNewMethod(int methodIndex)
        {
            // TODO: Record the method index (throw if duplicate)
            
            // Methods always start at multiple of 16 bytes
            WritePadding(16, _int3Padding);
        }

        /// <summary>
        /// Records that the current method is the entry point for the program.
        /// This must be called after <see cref="StartNewMethod"/> and before emitting any code for the method.
        /// Throws if the entry point is already set.
        /// </summary>
        public void MarkEntryPoint()
        {
            if (_entryPointOffset != 0)
                throw new InvalidOperationException("The entry point has already been set.");

            _entryPointOffset = (int)_exeStream.Position;
        }

        /// <summary>
        /// Creates the necessary section paddings and writes the PE header.
        /// This must be called after all methods and data have been emitted.
        /// </summary>
        public void FinalizeFile()
        {
            if (_entryPointOffset == 0)
                throw new InvalidOperationException("No entry point has been set.");

            var tempBuffer = new byte[4];

            // Pad the code section to file alignment
            WritePadding(FileAlignment, _int3Padding);

            // Store the padded code section size
            var sizeOfCode = (int)_exeStream.Position - PeHeaderSize;
            WriteIntAt(sizeOfCode, 156);

            // Store the entry point RVA
            var entryPointVirtualAddress = BaseOfCodeAddress - PeHeaderSize + _entryPointOffset;
            WriteIntAt(entryPointVirtualAddress, 168);
            
            void WriteIntAt(int value, int offset)
            {
                _exeStream.Seek(offset, SeekOrigin.Begin);
                
                // TODO: A bit of unsafe code to replace the arithmetic with a move
                tempBuffer[0] = (byte)value;
                tempBuffer[1] = (byte)(value >> 8);
                tempBuffer[2] = (byte)(value >> 16);
                tempBuffer[3] = (byte)(value >> 24);

                _exeStream.Write(tempBuffer, 0, 4);
            }
        }

        /// <summary>
        /// Inserts padding until the stream position is a multiple of <paramref name="multipleToPad"/>.
        /// The <paramref name="content"/> array is repeated (possibly partially).
        /// </summary>
        private void WritePadding(int multipleToPad, byte[] content)
        {
            // The last "% multipleToPad" is because the parenthesized expression may equal multipleToPad
            var bytesToPad = (multipleToPad - (int)_exeStream.Position % multipleToPad) % multipleToPad;

            while (bytesToPad > content.Length)
            {
                _exeStream.Write(content, 0, content.Length);
                bytesToPad -= content.Length;
            }
            _exeStream.Write(content, 0, bytesToPad);
        }

        // TODO: Once Stream.Write(ReadOnlySpan<byte>) overload is available, use the static data initialization trick
        private readonly byte[] _nullPadding =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        private readonly byte[] _int3Padding =
        {
            0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC
        };
    }
}
