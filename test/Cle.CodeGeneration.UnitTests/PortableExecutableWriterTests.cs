using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests
{
    public class PortableExecutableWriterTests
    {
        [Test]
        public void Ctor_reserves_space_for_header()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            Assert.That(stream.Position, Is.EqualTo(1024));
        }

        [Test]
        public void StartNewMethod_pads_to_16_byte_boundary()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            Assert.That(stream.Position, Is.EqualTo(1024));
            writer.StartNewMethod(0, "");
            Assert.That(stream.Position, Is.EqualTo(1024));
            writer.Emitter.EmitNop();
            writer.StartNewMethod(1, "");
            Assert.That(stream.Position, Is.EqualTo(1040));

            // The padding should consist of int 3's
            Assert.That(stream.GetBuffer()[1025], Is.EqualTo(0xCC));
        }

        [Test]
        public void StartNewMethod_writes_disassembly_header()
        {
            var stream = new MemoryStream();
            var disassembly = new StringWriter();
            var peWriter = new PortableExecutableWriter(stream, disassembly);
            
            peWriter.StartNewMethod(0, "Namespace::Method");
            Assert.That(disassembly.ToString(), Does.Contain("; Namespace::Method"));
        }

        [Test]
        public void FinalizeFile_stores_correct_entry_point()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            // Entry point is at file offset 1040 (0x410)
            writer.StartNewMethod(0, "");
            writer.Emitter.EmitNop();

            writer.StartNewMethod(1, "");
            writer.MarkEntryPoint();
            
            writer.FinalizeFile();

            // Address of the entry point should be stored relative to image base,
            // that is: base of code (fixed 0x1000) + file offset (0x410) - header size (0x400).
            // The information is stored at PE file offset 0xA8.
            var peSpan = stream.GetBuffer().AsSpan();
            var entryPointAddress = BitConverter.ToInt32(peSpan.Slice(168, 4));
            Assert.That(entryPointAddress, Is.EqualTo(0x1010));

            // The ".text" section should be 16 bytes in size
            // The total code size is stored at PE offset 0x9C.
            var codeSize = BitConverter.ToInt32(peSpan.Slice(156, 4));
            var textSectionFileSize = BitConverter.ToInt32(peSpan.Slice(408, 4));
            Assert.That(codeSize, Is.EqualTo(0x10));
            Assert.That(textSectionFileSize, Is.EqualTo(0x10));

            // When stored in memory, the header and two sections should take 4 KB each
            var inMemorySize = BitConverter.ToInt32(peSpan.Slice(208, 4));
            Assert.That(inMemorySize, Is.EqualTo(3*4096));
            var textSectionInMemorySize = BitConverter.ToInt32(peSpan.Slice(400, 4));
            Assert.That(textSectionInMemorySize, Is.EqualTo(4096));

            // The total file size should, therefore, be 1024 (header) + 512 (.text) + 512 (.idata) bytes
            Assert.That(stream.Length, Is.EqualTo(2048));
        }

        [Test]
        public void FinalizeFile_stores_correct_code_size_for_larger_program()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            // Create a couple file alignments' worth of methods
            // Entry point is at file offset 1024 (0x400)
            writer.MarkEntryPoint();
            for (var i = 0; i < 70; i++)
            {
                writer.StartNewMethod(i, "");
                writer.Emitter.EmitNop();
            }
            
            writer.FinalizeFile();

            var peSpan = stream.GetBuffer().AsSpan();
            var entryPointAddress = BitConverter.ToInt32(peSpan.Slice(168, 4));
            Assert.That(entryPointAddress, Is.EqualTo(0x1000));

            // 69 methods * 16 bytes/method + 1 unpadded method = 1105 bytes
            var codeSize = BitConverter.ToInt32(peSpan.Slice(156, 4));
            var textSectionFileSize = BitConverter.ToInt32(peSpan.Slice(408, 4));
            Assert.That(codeSize, Is.EqualTo(1105));
            Assert.That(textSectionFileSize, Is.EqualTo(1105));

            // Total: 1024 (header) + 1536 (.text) + 512 (.idata) bytes
            Assert.That(stream.Length, Is.EqualTo(3072));
        }

        [Test]
        public void FinalizeFile_applies_call_fixup()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            // Create two methods and a call between them
            writer.MarkEntryPoint();
            writer.StartNewMethod(0, "Main");
            writer.Emitter.EmitCallWithFixup(1, "Callee", out var fixup);
            writer.AddCallFixup(fixup);
            writer.StartNewMethod(1, "Callee");
            
            writer.FinalizeFile();

            // The first method starts at offset 0x400 and the second at 0x410
            // Therefore, the instruction at 0x400 is "call rip+0xB"
            var peSpan = stream.GetBuffer().AsSpan();
            var callInstruction = peSpan.Slice(0x400, 5).ToArray();
            CollectionAssert.AreEqual(new byte[] { 0xE8, 0x0B, 0x00, 0x00, 0x00 }, callInstruction);
        }

        [Test]
        public void FinalizeFile_applies_import_fixup()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            // Add a call to an imported method
            writer.MarkEntryPoint();
            writer.StartNewMethod(0, "Main");
            writer.Emitter.EmitCallIndirectWithFixup(1, "Callee", out var fixup);
            writer.AddCallFixup(fixup);
            writer.AddImport(1, Encoding.ASCII.GetBytes("method"), Encoding.ASCII.GetBytes("library"));
            
            writer.FinalizeFile();

            // The import table starts at offset 0x1000 relative to base of code.
            // There are 40 bytes for the import directory table, followed by 16 bytes of import lookup table.
            // The import address table is immediately after the import lookup table, before the name table.
            // Thus the desired import address will be at 0x1038.
            // The instruction at 0x400 is "call rip+0x1032".
            var peSpan = stream.GetBuffer().AsSpan();
            var callInstruction = peSpan.Slice(0x400, 6).ToArray();
            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x15, 0x32, 0x10, 0x00, 0x00 }, callInstruction);
        }

        [Test]
        public void FinalizeFile_updates_import_section_header()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            // Create 300 methods, each consuming 16 bytes.
            // This makes the .text section consume two memory pages, pushing .idata to 2*0x1000 relative to image base
            writer.MarkEntryPoint();
            for (var i = 0; i < 300; i++)
            {
                writer.StartNewMethod(i, "");
                writer.Emitter.EmitNop();
            }

            writer.AddImport(300, Encoding.ASCII.GetBytes("method"), Encoding.ASCII.GetBytes("library"));
            writer.AddImport(301, Encoding.ASCII.GetBytes("something"), Encoding.ASCII.GetBytes("other"));
            writer.AddImport(302, Encoding.ASCII.GetBytes("whatever"), Encoding.ASCII.GetBytes("library"));

            const int SectionSizeOnDisk = 3 * 20 // Import directory table, two entries plus one empty
                + 5 * 8 // Import lookup table, contains two null entries indicating end of list for some library
                + 5 * 8 // The same, but for import addresses
                + 10 + 12 + 12 // Hint/name table entries for the methods: 2 bytes + ASCII string + null + padding to even
                + 8 + 6; // Null-terminated ASCII strings for the library names

            writer.FinalizeFile();

            // The file should be 1024 (header) + 5120 (.text) + 512 (.idata) bytes long
            Assert.That(stream.Length, Is.EqualTo(6656));

            // The RVA table entry for the import table should have 0x2000 + base of code (0x1000) for the offset
            Assert.That(ReadIntAt(0x110), Is.EqualTo(0x3000));
            Assert.That(ReadIntAt(0x114), Is.EqualTo(SectionSizeOnDisk));

            // The .idata section header starts at file offset 0x1B0.
            Assert.That(ReadIntAt(0x1B8), Is.EqualTo(0x1000)); // Virtual size
            Assert.That(ReadIntAt(0x1BC), Is.EqualTo(0x3000)); // Virtual address relative to image base
            Assert.That(ReadIntAt(0x1C0), Is.EqualTo(SectionSizeOnDisk)); // Raw size
            Assert.That(ReadIntAt(0x1C4), Is.EqualTo(1024 + 5120)); // File pointer to raw data

            int ReadIntAt(int offset)
            {
                return BitConverter.ToInt32(stream.GetBuffer().AsSpan().Slice(offset, 4));
            }
        }

        [Test]
        public void FinalizeFile_adds_import_for_same_method_twice_if_requested()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);
            writer.MarkEntryPoint();
            writer.StartNewMethod(0, "");
            writer.Emitter.EmitNop();

            // Add two imports to the same method
            writer.AddImport(1, Encoding.ASCII.GetBytes("method"), Encoding.ASCII.GetBytes("library"));
            writer.AddImport(2, Encoding.ASCII.GetBytes("method"), Encoding.ASCII.GetBytes("library"));
            writer.FinalizeFile();

            // Check that the import section contains two entries, based on the size given in the RVA table
            const int SectionSizeOnDisk = 2 * 20 // Import directory table: one library plus null entry
                + 3 * 8 // Import lookup table: two entries plus one null entry
                + 3 * 8 // The same, but for import addresses
                + 10 + 10 // Hint/name table entries for the methods: 2 bytes + ASCII string + null + padding to even
                + 8; // Null-terminated ASCII strings for the library name
            Assert.That(BitConverter.ToInt32(stream.GetBuffer().AsSpan().Slice(0x114, 4)), Is.EqualTo(SectionSizeOnDisk));
        }

        [Test]
        public void FinalizeFile_coalesces_import_libraries_with_different_casing()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);
            writer.MarkEntryPoint();
            writer.StartNewMethod(0, "");
            writer.Emitter.EmitNop();

            // Add two imports to the same method
            writer.AddImport(1, Encoding.ASCII.GetBytes("method"), Encoding.ASCII.GetBytes("library"));
            writer.AddImport(2, Encoding.ASCII.GetBytes("method2"), Encoding.ASCII.GetBytes("LIBRARY"));
            writer.FinalizeFile();

            // Check that the import section contains two entries, based on the size given in the RVA table
            const int SectionSizeOnDisk = 2 * 20 // Import directory table: one library plus null entry
                + 3 * 8 // Import lookup table: two entries plus one null entry
                + 3 * 8 // The same, but for import addresses
                + 10 + 10 // Hint/name table entries for the methods: 2 bytes + ASCII string + null + padding to even
                + 8; // Null-terminated ASCII strings for the library name
            Assert.That(BitConverter.ToInt32(stream.GetBuffer().AsSpan().Slice(0x114, 4)), Is.EqualTo(SectionSizeOnDisk));
        }

        [Test]
        public void FinalizeFile_throws_if_entry_point_is_not_set()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);

            Assert.That(() => writer.FinalizeFile(), Throws.InvalidOperationException);
        }

        [Test]
        public void MarkEntryPoint_may_not_be_called_twice()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);
            writer.StartNewMethod(0, "");
            writer.MarkEntryPoint();

            Assert.That(() => writer.MarkEntryPoint(), Throws.InvalidOperationException);
        }

        [Test]
        public void TryGetMethodOffset_returns_offset_if_found()
        {
            var stream = new MemoryStream();
            var writer = new PortableExecutableWriter(stream, null);
            
            writer.MarkEntryPoint();
            writer.StartNewMethod(0, "Main");
            writer.Emitter.EmitNop();
            
            writer.StartNewMethod(1, "Other");
            writer.Emitter.EmitNop();

            Assert.That(writer.TryGetMethodOffset(0, out var first), Is.True);
            Assert.That(first, Is.GreaterThan(0));
            Assert.That(writer.TryGetMethodOffset(1, out var second), Is.True);
            Assert.That(second, Is.GreaterThan(first));
            Assert.That(writer.TryGetMethodOffset(2, out _), Is.False);
        }
    }
}
