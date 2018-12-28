using System.IO;
using NUnit.Framework;

namespace Cle.Frontend.UnitTests
{
    public class SourceFileProviderTests
    {
        private const string ModuleName = "SourceFileProviderTestCases";

        [OneTimeSetUp]
        public void SetUpFolderStructure()
        {
            Directory.Delete(ModuleName, true);
            Directory.CreateDirectory(ModuleName);
            var skippedFolder = Path.Combine(ModuleName, "ToBeSkipped");
            Directory.CreateDirectory(skippedFolder);

            using (var file = File.CreateText(Path.Combine(ModuleName, "file2.cle")))
            {
                file.Write("content");
            }
            using (File.Create(Path.Combine(ModuleName, "file1.cle"))) { }
            using (File.Create(Path.Combine(ModuleName, "ignored.notcle"))) { }
            using (File.Create(Path.Combine(skippedFolder, "ignored.cle"))) { }
        }

        [Test]
        public void Files_are_searched_in_default_directory_and_not_subdirectories()
        {
            var provider = new SourceFileProvider(Path.GetFullPath(ModuleName));

            Assert.That(provider.TryGetFilenamesForModule(".", out var filenames), Is.True);

            // The order must be fixed, and only .cle files in the exact directory must be found
            var expected1 = Path.GetFullPath(Path.Combine(ModuleName, "file1.cle"));
            var expected2 = Path.GetFullPath(Path.Combine(ModuleName, "file2.cle"));
            CollectionAssert.AreEqual(new[] { expected1, expected2 }, filenames);
        }

        [Test]
        public void Files_are_searched_in_specified_directory_and_not_subdirectories()
        {
            var provider = new SourceFileProvider(Directory.GetCurrentDirectory());

            Assert.That(provider.TryGetFilenamesForModule(ModuleName, out var filenames), Is.True);
            var expected1 = Path.GetFullPath(Path.Combine(ModuleName, "file1.cle"));
            var expected2 = Path.GetFullPath(Path.Combine(ModuleName, "file2.cle"));
            CollectionAssert.AreEqual(new[] { expected1, expected2 }, filenames);
        }

        [Test]
        public void Getting_filenames_fails_for_nonexistent_path()
        {
            var provider = new SourceFileProvider(Directory.GetCurrentDirectory());

            Assert.That(provider.TryGetFilenamesForModule("Nonexistent", out var _), Is.False);
        }

        [Test]
        public void Getting_file_contents_succeeds()
        {
            var provider = new SourceFileProvider(Directory.GetCurrentDirectory());

            var filename = Path.Combine(Directory.GetCurrentDirectory(), ModuleName, "file2.cle");
            Assert.That(provider.TryGetSourceFile(filename, out var bytes), Is.True);
            Assert.That(bytes.Length, Is.EqualTo(7));
        }

        [Test]
        public void Getting_file_contents_fails_if_file_does_not_exist()
        {
            var provider = new SourceFileProvider(ModuleName);

            Assert.That(provider.TryGetSourceFile("nonexistent.cle", out var _), Is.False);
        }

        [Test]
        public void Getting_file_contents_fails_if_file_is_in_use()
        {
            var provider = new SourceFileProvider(ModuleName);

            var path = Path.Combine(ModuleName, "InUse");
            Directory.CreateDirectory(path);
            var filename = Path.Combine(path, "inuse.cle");
            using (File.Create(filename))
            {
                Assert.That(provider.TryGetSourceFile(filename, out var _), Is.False);
            }

        }
    }
}
