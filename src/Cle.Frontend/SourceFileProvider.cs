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
            // TODO: Support for multiple modules
            if (moduleName != ".")
                throw new NotImplementedException("Support for modules");
            
            // TODO: Evaluate which exceptions can be handled (access denied, etc.)
            filenames = Directory.EnumerateFiles(_mainDirectory, "*.cle", SearchOption.AllDirectories);
            return true;
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
                // TODO: Is there some kind of exception that cannot be handled?
                // TODO: Should there be a way to message the type of error?
                fileBytes = Memory<byte>.Empty;
                return false;
            }
        }
    }
}
