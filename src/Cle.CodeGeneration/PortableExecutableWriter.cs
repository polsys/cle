using System;
using System.IO;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// This class handles the creation and emission of Portable Executable (.exe, .dll) metadata.
    /// </summary>
    internal sealed class PortableExecutableWriter
    {
        /// <summary>
        /// Gets the instruction emitter associated with this PE file.
        /// </summary>
        [NotNull]
        public X64Emitter Emitter { get; }

        [CanBeNull] private readonly TextWriter _disassemblyWriter;
        [NotNull] private readonly Stream _exeStream;
        private int _entryPointOffset;

        private const int FileAlignment = 0x0200; // 512 bytes, the minimum allowed
        private const int SectionAlignment = 0x1000; // 4096 bytes, the default page size
        private const int BaseOfCodeAddress = 0x00001000; // The default
        private const int PeHeaderSize = 0x0400; // 1024 bytes, a safe bet
        
        public PortableExecutableWriter([NotNull] Stream outputStream, [CanBeNull] TextWriter disassemblyWriter)
        {
            Emitter = new X64Emitter(outputStream, disassemblyWriter);
            _exeStream = outputStream;
            _disassemblyWriter = disassemblyWriter;

            // Write the initial header with to-be-filled entries set to zero
            // In total, the header takes 1024 bytes
            _exeStream.Write(_skeletonPeHeader, 0, _skeletonPeHeader.Length);
            _exeStream.Seek(1024, SeekOrigin.Begin);
        }

        /// <summary>
        /// Prepares the emitter for a new method, by inserting appropriate padding and
        /// recording the mapping between method index and address.
        /// </summary>
        /// <param name="methodIndex">The compiler internal method index.</param>
        /// <param name="methodName">The full method name, used for disassembly.</param>
        public void StartNewMethod(int methodIndex, [NotNull] string methodName)
        {
            // TODO: Record the method index (throw if duplicate)
            
            // Methods always start at multiple of 16 bytes
            WritePadding(16, _int3Padding);

            // Write a header for method disassembly
            if (_disassemblyWriter != null)
            {
                _disassemblyWriter.WriteLine();
                _disassemblyWriter.WriteLine();
                _disassemblyWriter.Write("; ");
                _disassemblyWriter.WriteLine(methodName);
            }
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
            var paddedSizeOfCode = (int)_exeStream.Position - PeHeaderSize;
            WriteIntAt(paddedSizeOfCode, 156); // Image header: size of code
            WriteIntAt(RoundUp(paddedSizeOfCode, SectionAlignment), 400); // .text section: in-memory size
            WriteIntAt(paddedSizeOfCode, 408); // .text section: file size

            // Store the entry point RVA
            var entryPointVirtualAddress = BaseOfCodeAddress - PeHeaderSize + _entryPointOffset;
            WriteIntAt(entryPointVirtualAddress, 168);

            // Store the image size as multiple of section alignment
            // PE header + .text
            var totalImageSize = SectionAlignment + RoundUp(paddedSizeOfCode, SectionAlignment);
            WriteIntAt(totalImageSize, 208);
            
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
            var bytesToPad = RoundUp((int)_exeStream.Position, multipleToPad) - (int)_exeStream.Position;

            while (bytesToPad > content.Length)
            {
                _exeStream.Write(content, 0, content.Length);
                bytesToPad -= content.Length;
            }
            _exeStream.Write(content, 0, bytesToPad);
        }

        private static int RoundUp(int size, int alignment)
        {
            // The last "% alignment" is because the expression within parens may equal alignment
            return size + (alignment - size % alignment) % alignment;
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

        private readonly byte[] _skeletonPeHeader =
        {
            // MS-DOS header (http://www.delorie.com/djgpp/doc/exe/)
            (byte)'M', (byte)'Z', 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
            0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00,
            // 32 bytes of null
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // File offset of the PE header: 0x00000080 (stored little-endian, of course)
            0x80, 0x00, 0x00, 0x00,
            // MS-DOS stub program
            0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21,
            (byte)'T', (byte)'h', (byte)'i', (byte)'s', (byte)' ', (byte)'p', (byte)'r', (byte)'o', (byte)'g', (byte)'r',
            (byte)'a', (byte)'m', (byte)' ', (byte)'c', (byte)'a', (byte)'n', (byte)'n', (byte)'o', (byte)'t', (byte)' ',
            (byte)'b', (byte)'e', (byte)' ', (byte)'r', (byte)'u', (byte)'n', (byte)' ', (byte)'i', (byte)'n', (byte)' ',
            (byte)'D', (byte)'O', (byte)'S', (byte)' ', (byte)'m', (byte)'o', (byte)'d', (byte)'e', (byte)'.',
            0x0D, 0x0D, 0x0A, 0x24,
            // Padding
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

            // PE header
            (byte)'P', (byte)'E', 0x00, 0x00, // Magic
            0x64, 0x86, // Machine type: x86-64
            0x01, 0x00, // Number of sections - TODO Update this when adding data sections
            0x00, 0x00, 0x00, 0x00, // Time stamp
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Deprecated (symbol table information)
            0xF0, 0x00, // Size of optional header
            // Image characteristics:
            //   0x0001 RELOCS_STRIPPED
            //   0x0002 EXECUTABLE_IMAGE
            //   0x0200 DEBUG_STRIPPED
            0x03, 0x02,

            // Image header - standard fields
            0x0B, 0x02, // Magic for PE32+
            0x01, 0x00, // Linker version: 1.0
            0x00, 0x00, 0x00, 0x00, // Size of code - filled by FinalizeFile()
            0x00, 0x00, 0x00, 0x00, // Size of initialized data - filled by FinalizeFile()
            0x00, 0x00, 0x00, 0x00, // Size of uninitialized data - filled by FinalizeFile()
            0x00, 0x00, 0x00, 0x00, // Address of entry point - filled by FinalizeFile()
            0x00, 0x10, 0x00, 0x00, // Base of code

            // Image header - Windows-specific fields
            0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, // Image base
            0x00, 0x10, 0x00, 0x00, // Section alignment - 4 KB
            0x00, 0x02, 0x00, 0x00, // File alignment - 512 bytes
            0x06, 0x00, 0x00, 0x00, // Required OS version: 6.0
            0x00, 0x00, 0x00, 0x00, // Image version
            0x06, 0x00, 0x00, 0x00, // Required subsystem version: 6.0
            0x00, 0x00, 0x00, 0x00, // Reserved
            0x00, 0x00, 0x00, 0x00, // Size of image loaded in memory - filled by FinalizeFile()
            0x00, 0x04, 0x00, 0x00, // Size of headers rounded up to file alignment
            0x00, 0x00, 0x00, 0x00, // Checksum
            0x03, 0x00, // Subsystem: WINDOWS_CUI
            // DLL characteristics:
            //   0x0020 HIGH_ENTROPY_VA
            //   0x0100 NX_COMPAT
            //   0x0400 NO_SEH
            //   0x8000 TERMINAL_SERVER_AWARE
            0x20, 0x85,
            0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, // Stack reserve: 1 MB
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Stack commit: 4 KB
            0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, // Heap reserve: 1 MB
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Heap commit: 4 KB
            0x00, 0x00, 0x00, 0x00, // Reserved
            0x10, 0x00, 0x00, 0x00, // Number of RVAs: all 16 documented

            // The RVA table
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Exports
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Imports
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Resources
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Exceptions
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Certificates
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Base relocs
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Debug
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Reserved
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Global pointer
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // TLS
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Load config
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Bound import
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Import addresses
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Delay imports
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // CLR runtime header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Reserved
            
            // .text section header
            (byte)'.', (byte)'t', (byte)'e', (byte)'x', (byte)'t', 0x00, 0x00, 0x00, // Name
            0x00, 0x00, 0x00, 0x00, // Virtual size - filled by FinalizeFile()
            0x00, 0x10, 0x00, 0x00, // Virtual address relative to image base
            0x00, 0x00, 0x00, 0x00, // Raw size - filled by FinalizeFile()
            0x00, 0x04, 0x00, 0x00, // Pointer to raw data - immediately after the headers
            0x00, 0x00, 0x00, 0x00, // Pointer to relocations
            0x00, 0x00, 0x00, 0x00, // Deprecated (pointer to line numbers)
            0x00, 0x00, // Number of relocations
            0x00, 0x00, // Deprecated (number of line numbers)
            // Section characteristics:
            //   0x00000020 CNT_CODE
            //   0x20000000 MEM_EXECUTE
            //   0x40000000 MEM_READ
            0x20, 0x00, 0x00, 0x60,
        };
    }
}
