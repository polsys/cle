using System.IO;
using Cle.Compiler;

namespace Cle.Benchmarks.Compiler
{
    internal class NullOutputProvider : IOutputFileProvider
    {
        public Stream? GetExecutableStream()
        {
            return new EmptyStream();
        }

        public TextWriter? GetDisassemblyWriter()
        {
            return null;
        }

        public TextWriter? GetDebugFileWriter()
        {
            return null;
        }
    }
}
