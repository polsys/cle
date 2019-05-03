using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class TestingSourceFileProvider : ISourceFileProvider
    {
        private readonly Dictionary<string, List<string>> _moduleFiles = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, byte[]> _fileContents = new Dictionary<string, byte[]>();

        private readonly HashSet<string> _readFiles = new HashSet<string>();

        /// <summary>
        /// Adds a new source file to the emulated file system.
        /// If the module does not yet exist, it is created.
        /// If <paramref name="source"/> is null, the file is created as inaccessible.
        /// </summary>
        /// <param name="moduleName">The name of the module the file belongs to.</param>
        /// <param name="filename">Unique file name.</param>
        /// <param name="source">
        /// File contents as an UTF-16 string. The contents will be converted to UTF-8.
        /// Specify null to create a missing file.
        /// </param>
        public void Add(string moduleName, string filename, string? source)
        {
            if (!_moduleFiles.ContainsKey(moduleName))
            {
                _moduleFiles.Add(moduleName, new List<string>());
            }

            _moduleFiles[moduleName].Add(filename);
            if (source != null)
            {
                _fileContents.Add(filename, Encoding.UTF8.GetBytes(source));
            }
        }

        public bool TryGetFilenamesForModule(string moduleName, out IEnumerable<string>? filenames)
        {
            if (_moduleFiles.TryGetValue(moduleName, out var names))
            {
                filenames = names;
                return true;
            }
            else
            {
                filenames = null;
                return false;
            }
        }

        public bool TryGetSourceFile(string filename, out Memory<byte> fileBytes)
        {
            _readFiles.Add(filename);

            if (_fileContents.TryGetValue(filename, out var bytes))
            {
                fileBytes = bytes.AsMemory();
                return true;
            }
            else
            {
                fileBytes = Memory<byte>.Empty;
                return false;
            }
        }

        /// <summary>
        /// Asserts that the given file has been accessed.
        /// </summary>
        public void AssertFileWasRead(string filename)
        {
            Assert.That(_readFiles.Contains(filename), Is.True, $"File {filename} was not accessed");
        }
    }
}
