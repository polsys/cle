using System;
using System.IO;

namespace Cle.Compiler.UnitTests
{
    internal class TestingOutputFileProvider : IOutputFileProvider, IDisposable
    {
        /// <summary>
        /// Gets the mock output stream.
        /// Null if never requested via <see cref="GetExecutableStream"/>.
        /// </summary>
        public MemoryStream? ExecutableStream { get; private set; }

        /// <summary>
        /// If true, <see cref="GetExecutableStream"/> will always return <c>null</c>.
        /// </summary>
        public bool FailExecutable { get; set; }

        /// <summary>
        /// If true, <see cref="GetDisassemblyWriter"/> will always return <c>null</c>.
        /// </summary>
        public bool FailDisassembly { get; set; }

        /// <summary>
        /// Gets the mock disassembly writer.
        /// Null if never requested via <see cref="GetDisassemblyWriter"/>.
        /// </summary>
        public StringWriter? DisassemblyWriter { get; private set; }

        /// <summary>
        /// Gets the mock debug log writer.
        /// Null if never requested via <see cref="GetDebugFileWriter"/>.
        /// </summary>
        public StringWriter? DebugWriter { get; private set; }

        public void Dispose()
        {
            ExecutableStream?.Dispose();
            DisassemblyWriter?.Dispose();
            DebugWriter?.Dispose();
        }

        public Stream? GetExecutableStream()
        {
            if (FailExecutable)
                return null;

            return ExecutableStream ?? (ExecutableStream = new MemoryStream());
        }

        public TextWriter? GetDisassemblyWriter()
        {
            if (FailDisassembly)
                return null;

            return DisassemblyWriter ?? (DisassemblyWriter = new StringWriter());
        }

        public TextWriter? GetDebugFileWriter()
        {
            return DebugWriter ?? (DebugWriter = new StringWriter());
        }
    }
}
