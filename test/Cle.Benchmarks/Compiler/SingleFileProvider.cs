using System;
using System.Collections.Generic;
using System.Text;
using Cle.Compiler;

namespace Cle.Benchmarks.Compiler
{
    internal class SingleFileProvider : ISourceFileProvider
    {
        private static readonly string[] s_filenames = { "file.cle" };
        private readonly Memory<byte> _source;

        public SingleFileProvider(string source)
        {
            _source = Encoding.UTF8.GetBytes(source).AsMemory();
        }

        public bool TryGetFilenamesForModule(string moduleName, out IEnumerable<string>? filenames)
        {
            filenames = s_filenames;
            return true;
        }

        public bool TryGetSourceFile(string filename, out Memory<byte> fileBytes)
        {
            if (filename == "file.cle")
            {
                fileBytes = _source;
                return true;
            }
            else
            {
                fileBytes = Memory<byte>.Empty;
                return false;
            }
        }
    }
}
