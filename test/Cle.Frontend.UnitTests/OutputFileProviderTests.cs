using System.IO;
using NUnit.Framework;

namespace Cle.Frontend.UnitTests
{
    public class OutputFileProviderTests
    {
        private const string DumpFileName = "cle-dump.txt";
        private const string PlatformName = "windows-x64";
        private const string OutputName = "filename";
        private const string BinDirectoryName = "_bin/" + PlatformName;
        private const string ExecutableExtension = ".exe";
        private const string ExecutableFileName = BinDirectoryName + "/" + OutputName + ExecutableExtension;
        private const string DisassemblyFileName = BinDirectoryName + "/" + OutputName + ".asm";

        [Test]
        public void GetDebugFileWriter_returns_writer()
        {
            using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
            {
                var writer = provider.GetDebugFileWriter();
                Assert.That(writer, Is.Not.Null);
                Assert.That(File.Exists(DumpFileName), Is.True);

                // The call should be idempotent
                Assert.That(provider.GetDebugFileWriter(), Is.SameAs(writer));
            }

            File.Delete(DumpFileName);
        }

        [Test]
        public void GetDebugFileWriter_returns_null_if_file_cannot_be_created()
        {
            using (var _ = File.Create(DumpFileName))
            {
                using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
                {
                    Assert.That(provider.GetDebugFileWriter(), Is.Null);
                }
            }
            File.Delete(DumpFileName);
        }

        [Test]
        public void GetExecutableStream_creates_file_in_correct_path()
        {
            using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
            {
                var stream = provider.GetExecutableStream();
                Assert.That(stream, Is.Not.Null);
                Assert.That(File.Exists(ExecutableFileName), Is.True);

                // The call should be idempotent
                Assert.That(provider.GetExecutableStream(), Is.SameAs(stream));
            }

            Directory.Delete(BinDirectoryName, true);
        }

        [Test]
        public void GetExecutableStream_does_not_duplicate_file_extension()
        {
            // In case the user provides output name file "out.exe", we should not add unnecessary extension
            using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName + ExecutableExtension))
            {
                var stream = provider.GetExecutableStream();
                Assert.That(stream, Is.Not.Null);

                // Instead of "filename.exe.exe", should just be "filename.exe"
                Assert.That(File.Exists(ExecutableFileName), Is.True);
            }

            Directory.Delete(BinDirectoryName, true);
        }

        [Test]
        public void GetExecutableStream_returns_null_if_file_cannot_be_created()
        {
            Directory.CreateDirectory(BinDirectoryName);
            using (var _ = File.Create(ExecutableFileName))
            {
                using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
                {
                    Assert.That(provider.GetExecutableStream(), Is.Null);
                }
            }

            Directory.Delete(BinDirectoryName, true);
        }

        [Test]
        public void GetDisassemblyWriter_creates_file_in_correct_path()
        {
            using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
            {
                var stream = provider.GetDisassemblyWriter();
                Assert.That(stream, Is.Not.Null);
                Assert.That(File.Exists(DisassemblyFileName), Is.True);

                // The call should be idempotent
                Assert.That(provider.GetDisassemblyWriter(), Is.SameAs(stream));
            }

            Directory.Delete(BinDirectoryName, true);
        }

        [Test]
        public void GetDisassemblyWriter_returns_null_if_file_cannot_be_created()
        {
            Directory.CreateDirectory(BinDirectoryName);
            using (var _ = File.Create(DisassemblyFileName))
            {
                using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory(), OutputName))
                {
                    Assert.That(provider.GetDisassemblyWriter(), Is.Null);
                }
            }

            Directory.Delete(BinDirectoryName, true);
        }
    }
}
