using System;
using System.IO;
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

            // The ".text" section should be 512 bytes in size (we use the lowest allowed power of 2)
            // The total code size is stored at PE offset 0x9C.
            var codeSize = BitConverter.ToInt32(peSpan.Slice(156, 4));
            var textSectionFileSize = BitConverter.ToInt32(peSpan.Slice(408, 4));
            Assert.That(codeSize, Is.EqualTo(0x200));
            Assert.That(textSectionFileSize, Is.EqualTo(0x200));

            // When stored in memory, the header and single section should take 4 KB each
            var inMemorySize = BitConverter.ToInt32(peSpan.Slice(208, 4));
            Assert.That(inMemorySize, Is.EqualTo(8192));
            var textSectionInMemorySize = BitConverter.ToInt32(peSpan.Slice(400, 4));
            Assert.That(textSectionInMemorySize, Is.EqualTo(4096));

            // The total file size should, therefore, be 1024 (header) + 512 (.text) bytes
            Assert.That(stream.Length, Is.EqualTo(1536));
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

            // 70 methods * 16 bytes/method = 3 file alignments = 1536 bytes
            var codeSize = BitConverter.ToInt32(peSpan.Slice(156, 4));
            var textSectionFileSize = BitConverter.ToInt32(peSpan.Slice(408, 4));
            Assert.That(codeSize, Is.EqualTo(0x600));
            Assert.That(textSectionFileSize, Is.EqualTo(0x600));

            // Total: 1024 (header) + 1536 (.text) bytes
            Assert.That(stream.Length, Is.EqualTo(2560));
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
    }
}
