using System;
using System.IO;

namespace Cle.Compiler.UnitTests
{
    internal class TestingOutputFileProvider : IOutputFileProvider, IDisposable
    {
        /// <summary>
        /// Gets the mock debug log writer.
        /// Null if never requested via <see cref="GetDebugFileWriter"/>.
        /// </summary>
        public StringWriter DebugWriter { get; private set; }

        public void Dispose()
        {
            DebugWriter?.Dispose();
        }

        public TextWriter GetDebugFileWriter()
        {
            if (DebugWriter is null)
            {
                DebugWriter = new StringWriter();
            }

            return DebugWriter;
        }
    }
}
