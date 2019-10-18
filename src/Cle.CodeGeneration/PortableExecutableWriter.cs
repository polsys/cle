using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        public X64Emitter Emitter { get; }

        private readonly TextWriter? _disassemblyWriter;
        private readonly Stream _exeStream;
        private int _entryPointOffset;

        private readonly List<Fixup> _callFixups = new List<Fixup>();
        // TODO: Consider an array instead
        private readonly Dictionary<int, int> _methodOffsets = new Dictionary<int, int>();

        private readonly Dictionary<string, List<(int methodIndex, byte[] methodName)>> _imports
            = new Dictionary<string, List<(int, byte[])>>();

        private int _currentSectionRva = BaseOfCodeAddress; // Assuming that the .text section comes first
        private int _currentSectionFileAddress;
        private int _currentSectionIndex;

        private readonly byte[] _tempBufferForWriteIntOnly = new byte[4];

        private const int FileAlignment = 0x0200; // 512 bytes, the minimum allowed
        private const int SectionAlignment = 0x1000; // 4096 bytes, the default page size
        private const int BaseOfCodeAddress = 0x00001000; // The default
        private const int PeHeaderSize = 0x0400; // 1024 bytes, a safe bet
        
        public PortableExecutableWriter(Stream outputStream, TextWriter? disassemblyWriter)
        {
            Emitter = new X64Emitter(outputStream, disassemblyWriter);
            _exeStream = outputStream;
            _disassemblyWriter = disassemblyWriter;

            // Write the initial header with to-be-filled entries set to zero
            // In total, the header takes 1024 bytes
            _exeStream.Write(SkeletonPeHeader);
            _exeStream.Seek(1024, SeekOrigin.Begin);
            _currentSectionFileAddress = (int)_exeStream.Position;
        }

        /// <summary>
        /// Prepares the emitter for a new method, by inserting appropriate padding and
        /// recording the mapping between method index and address.
        /// </summary>
        /// <param name="methodIndex">The compiler internal method index.</param>
        /// <param name="methodName">The full method name, used for disassembly.</param>
        public void StartNewMethod(int methodIndex, string methodName)
        {
            // Methods always start at multiple of 16 bytes
            WritePadding(16, Int3Padding);
            _methodOffsets.Add(methodIndex, (int)_exeStream.Position);

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
        /// If the specified method has been started with <see cref="StartNewMethod"/>,
        /// returns true and the file offset for the method. Otherwise returns false.
        /// </summary>
        /// <param name="methodIndex">The compiler-internal index passed to <see cref="StartNewMethod"/>.</param>
        /// <param name="offset">The file offset at the start of the method.</param>
        public bool TryGetMethodOffset(int methodIndex, out int offset)
        {
            return _methodOffsets.TryGetValue(methodIndex, out offset);
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
        /// Adds an entry to the import table.
        /// The table is written out when <see cref="FinalizeFile"/> is called.
        /// </summary>
        /// <param name="methodIndex">The index used for referring to this method.</param>
        /// <param name="methodName">
        /// ASCII-encoded method name.
        /// Does not need to be unique even within library: multiple imports will be emitted.
        /// </param>
        /// <param name="libraryName">
        /// ASCII-encoded library name.
        /// Imports from a single library will be coalesced into one import table entry.
        /// </param>
        public void AddImport(int methodIndex, byte[] methodName, byte[] libraryName)
        {
            // For sensible comparisons, here we must finally allocate a string for the library name
            // The syntax tree and IR do not do so, to preserve the information as is
            var libraryString = Encoding.ASCII.GetString(libraryName).ToLowerInvariant();

            if (!_imports.TryGetValue(libraryString, out var importList))
            {
                importList = new List<(int methodIndex, byte[] methodName)>();
                _imports.Add(libraryString, importList);
            }

            importList.Add((methodIndex, methodName));
        }

        /// <summary>
        /// Records a method call fixup to be applied when <see cref="FinalizeFile"/> is called.
        /// </summary>
        /// <param name="fixup">The tag must contain the callee method index.</param>
        public void AddCallFixup(in Fixup fixup)
        {
            _callFixups.Add(fixup);
        }

        /// <summary>
        /// Creates the necessary section padding, writes import and data sections and writes the PE header.
        /// This must be called after all methods, imports and data have been emitted.
        /// </summary>
        public void FinalizeFile()
        {
            if (_entryPointOffset == 0)
                throw new InvalidOperationException("No entry point has been set.");

            // Pad the code section to file alignment
            var (_, codeFileSize) = EndSection(Int3Padding);
            var inMemorySizeOfCode = RoundUp(codeFileSize, SectionAlignment);

            // Emit the import section
            EmitImports(_currentSectionRva);
            var (importRva, importFileSize) = EndSection(ZeroPadding);
            var inMemorySizeOfImports = RoundUp(importFileSize, SectionAlignment);

            // Optional header
            WriteIntAt(codeFileSize, 156); // Size of code
            WriteIntAt(importFileSize, 160); // Size of initialized data

            // Store the entry point RVA
            var entryPointVirtualAddress = BaseOfCodeAddress - PeHeaderSize + _entryPointOffset;
            WriteIntAt(entryPointVirtualAddress, 168);

            // Store the image size as multiple of section alignment
            // PE header + .text + .idata
            var totalImageSize = SectionAlignment + inMemorySizeOfCode + inMemorySizeOfImports;
            WriteIntAt(totalImageSize, 208);

            // Store the address and size of the import directory in the RVA table
            WriteIntAt(importRva, 272);
            WriteIntAt(importFileSize, 276);

            // Apply the last fixups
            ApplyCallFixups();
        }

        private (int startVirtualAddress, int fileSize) EndSection(ReadOnlySpan<byte> padding)
        {
            var startVirtualAddress = _currentSectionRva;

            // Pad the section to file alignment
            var fileSize = (int)_exeStream.Length - _currentSectionFileAddress;
            WritePadding(FileAlignment, padding);

            // Update the appropriate section header
            var sectionHeaderPosition = 0x190 + 0x28 * _currentSectionIndex;
            WriteIntAt(RoundUp(fileSize, SectionAlignment), sectionHeaderPosition);
            WriteIntAt(startVirtualAddress, sectionHeaderPosition + 4);
            WriteIntAt(fileSize, sectionHeaderPosition + 8);
            WriteIntAt(_currentSectionFileAddress, sectionHeaderPosition + 12);

            // Update values for the next section
            _exeStream.Seek(0, SeekOrigin.End);
            _currentSectionIndex++;
            _currentSectionFileAddress = (int)_exeStream.Position;
            _currentSectionRva += RoundUp(fileSize, SectionAlignment);

            return (startVirtualAddress, fileSize);
        }

        private void EmitImports(int importTableRva)
        {
            var fileToRvaOffset = importTableRva - (int)_exeStream.Position;

            // Determine the total size of the fixed-size tables
            var directoryCount = _imports.Count + 1; // Empty entry marks the end of list
            var importEntryCount = 0;
            foreach (var directory in _imports)
            {
                // Again, an empty entry for each lookup table
                importEntryCount += directory.Value.Count + 1;
            }

            // Write the import directory table
            // We can set most values since the data structures have fixed size,
            // but we need to come back for the library name addresses
            var currentLookupTablePosition = importTableRva + 20 * directoryCount;
            var addressTableStart = currentLookupTablePosition + 8 * importEntryCount;
            var currentAddressTablePosition = addressTableStart;

            foreach (var directory in _imports)
            {
                WriteInt(currentLookupTablePosition); // Lookup table RVA
                WriteZeros(12); // Time stamp, forwarder chain, name RVA (this is fixed up later)
                WriteInt(currentAddressTablePosition); // Import address table RVA

                var lookupTableSize = (directory.Value.Count + 1) * 8;
                currentLookupTablePosition += lookupTableSize;
                currentAddressTablePosition += lookupTableSize;
            }

            // Write the last empty import directory
            WriteZeros(20);

            // Write the lookup table and the address table.
            // These should have exactly same contents.
            // Also store address table locations for method call fixups.
            var nameTableContents = new List<int>(importEntryCount);
            var currentNameTablePosition = currentAddressTablePosition;
            currentAddressTablePosition = addressTableStart;

            foreach (var directory in _imports)
            {
                foreach (var (methodIndex, methodName) in directory.Value)
                {
                    nameTableContents.Add(currentNameTablePosition);

                    // We have to fix up the position because the emitter uses file offsets, not RVA offsets
                    _methodOffsets.Add(methodIndex, currentAddressTablePosition - (SectionAlignment - PeHeaderSize));

                    currentNameTablePosition += CalculateNameSize(methodName.Length);
                    currentAddressTablePosition += 8;
                }

                // An empty entry indicates the end of list
                nameTableContents.Add(0);
                currentAddressTablePosition += 8;
            }

            // Then write the contents twice: first for name table and then for address table
            for (var i = 0; i < 2; i++)
            {
                foreach (var entry in nameTableContents)
                {
                    // The name entry RVA is stored as low dword of a 64-bit value
                    WriteInt(entry);
                    WriteInt(0);
                }
            }

            // Write the name table
            foreach (var directory in _imports)
            {
                foreach (var (_, methodName) in directory.Value)
                {
                    // Ordinal hint: we have it empty
                    _exeStream.WriteByte(0);
                    _exeStream.WriteByte(0);

                    // The name
                    _exeStream.Write(methodName.AsSpan());

                    // A null terminator, and possibly padding to even
                    _exeStream.WriteByte(0);
                    if (_exeStream.Position % 2 != 0)
                    {
                        _exeStream.WriteByte(0);
                    }
                }
            }

            // Write the library names
            var currentNameFixupPosition = importTableRva - fileToRvaOffset + 12;
            foreach (var directory in _imports)
            {
                var nameRva = fileToRvaOffset + (int)_exeStream.Position;
                _exeStream.Write(Encoding.ASCII.GetBytes(directory.Key));
                _exeStream.WriteByte(0);

                // Fix up the name
                WriteIntAt(nameRva, currentNameFixupPosition);
                _exeStream.Seek(0, SeekOrigin.End);
                currentNameFixupPosition += 20;
            }

            static int CalculateNameSize(int nameLength)
            {
                // Two bytes for ordinal hint, the name bytes, a null byte, pad to even
                return RoundUp(2 + nameLength + 1, 2);
            }
        }

        private void ApplyCallFixups()
        {
            foreach (var fixup in _callFixups)
            {
                Emitter.ApplyFixup(fixup, _methodOffsets[fixup.Tag]);
            }
        }

        private void WriteInt(int value)
        {
            // Perf: As of 18-Oct-2019, this is significantly faster than a stackalloc Span, and slightly faster
            // than using unsafe code as in X64Emitter (but not much, and for ulongs unsafe is faster)
            _tempBufferForWriteIntOnly[0] = (byte)value;
            _tempBufferForWriteIntOnly[1] = (byte)(value >> 8);
            _tempBufferForWriteIntOnly[2] = (byte)(value >> 16);
            _tempBufferForWriteIntOnly[3] = (byte)(value >> 24);

            _exeStream.Write(_tempBufferForWriteIntOnly);
        }

        private void WriteIntAt(int value, int offset)
        {
            _exeStream.Seek(offset, SeekOrigin.Begin);
            WriteInt(value);
        }

        private void WriteZeros(int byteCount)
        {
            while (byteCount > ZeroPadding.Length)
            {
                _exeStream.Write(ZeroPadding);
                byteCount -= ZeroPadding.Length;
            }

            _exeStream.Write(ZeroPadding.Slice(0, byteCount));
        }

        /// <summary>
        /// Inserts padding until the stream position is a multiple of <paramref name="multipleToPad"/>.
        /// The <paramref name="content"/> array is repeated (possibly partially).
        /// </summary>
        private void WritePadding(int multipleToPad, ReadOnlySpan<byte> content)
        {
            var bytesToPad = RoundUp((int)_exeStream.Position, multipleToPad) - (int)_exeStream.Position;

            while (bytesToPad > content.Length)
            {
                _exeStream.Write(content);
                bytesToPad -= content.Length;
            }
            _exeStream.Write(content.Slice(0, bytesToPad));
        }

        private static int RoundUp(int size, int alignment)
        {
            // The last "% alignment" is because the expression within parens may equal alignment
            return size + (alignment - size % alignment) % alignment;
        }

        private static ReadOnlySpan<byte> ZeroPadding => new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static ReadOnlySpan<byte> Int3Padding => new byte[]
        {
            0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC
        };

        private static ReadOnlySpan<byte> SkeletonPeHeader => new byte[]
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
            0x02, 0x00, // Number of sections - TODO Update this when adding data sections
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
            0x00, 0x00, 0x00, 0x00, // Virtual size - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Virtual address relative to image base - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Raw size - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Pointer to raw data - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Pointer to relocations
            0x00, 0x00, 0x00, 0x00, // Deprecated (pointer to line numbers)
            0x00, 0x00, // Number of relocations
            0x00, 0x00, // Deprecated (number of line numbers)
            // Section characteristics:
            //   0x00000020 CNT_CODE
            //   0x20000000 MEM_EXECUTE
            //   0x40000000 MEM_READ
            0x20, 0x00, 0x00, 0x60,
            
            // .idata section header
            (byte)'.', (byte)'i', (byte)'d', (byte)'a', (byte)'t', (byte)'a', 0x00, 0x00, // Name
            0x00, 0x00, 0x00, 0x00, // Virtual size - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Virtual address relative to image base - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Raw size - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Pointer to raw data - filled by EndSection()
            0x00, 0x00, 0x00, 0x00, // Pointer to relocations
            0x00, 0x00, 0x00, 0x00, // Deprecated (pointer to line numbers)
            0x00, 0x00, // Number of relocations
            0x00, 0x00, // Deprecated (number of line numbers)
            // Section characteristics:
            //   0x00000040 CNT_INITIALIZED_DATA
            //   0x40000000 MEM_READ
            //   0x80000000 MEM_WRITE
            0x40, 0x00, 0x00, 0xC0,
        };
    }
}
