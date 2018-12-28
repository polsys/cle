using System;
using System.Collections.Generic;
using System.IO;
using Cle.Compiler;
using JetBrains.Annotations;

namespace Cle.Frontend
{
    internal class SourceFileProvider: ISourceFileProvider
    {
        private readonly string _mainDirectory;

        public SourceFileProvider([NotNull] string mainDirectory)
        {
            _mainDirectory = mainDirectory;
        }

        public bool TryGetFilenamesForModule(string moduleName, out IEnumerable<string> filenames)
        {
            // Use absolute paths everywhere
            // TODO: Support for module search paths
            try
            {
                filenames = Directory.EnumerateFiles(Path.GetFullPath(Path.Combine(_mainDirectory, moduleName)),
                    "*.cle", SearchOption.TopDirectoryOnly);
                return true;
            }
            catch (IOException)
            {
                // TODO: Is there some kind of IO exception that should not be handled?
                filenames = null;
                return false;
            }
        }

        public bool TryGetSourceFile(string filename, out Memory<byte> fileBytes)
        {
            try
            {
                fileBytes = File.ReadAllBytes(filename).AsMemory();
                return true;
            }
            catch (IOException)
            {
                // TODO: Is there some kind of IO exception that should not be handled?
                // TODO: Should there be a way to message the type of error?
                fileBytes = Memory<byte>.Empty;
                return false;
            }
        }
    }
}
