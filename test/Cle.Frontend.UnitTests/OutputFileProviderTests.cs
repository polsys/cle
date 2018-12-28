using System.IO;
using NUnit.Framework;

namespace Cle.Frontend.UnitTests
{
    public class OutputFileProviderTests
    {
        private const string DumpFileName = "cle-dump.txt";

        [Test]
        public void GetDebugFileWriter_returns_writer()
        {
            using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory()))
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
            using (var blockingFile = File.Create(DumpFileName))
            {
                using (var provider = new OutputFileProvider(Directory.GetCurrentDirectory()))
                {
                    Assert.That(provider.GetDebugFileWriter(), Is.Null);
                }
            }
            File.Delete(DumpFileName);
        }
    }
}
