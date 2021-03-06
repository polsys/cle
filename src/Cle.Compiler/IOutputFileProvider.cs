﻿using System.IO;

namespace Cle.Compiler
{
    /// <summary>
    /// Provides methods for creating output file streams.
    /// The lifetime is handled by the creator.
    /// </summary>
    public interface IOutputFileProvider
    {
        /// <summary>
        /// Gets the writable stream for the output executable.
        /// </summary>
        Stream? GetExecutableStream();

        /// <summary>
        /// Gets a writer for the output disassembly.
        /// May return null if the file can not be created.
        /// </summary>
        TextWriter? GetDisassemblyWriter();

        /// <summary>
        /// Gets a debug log writer.
        /// May return null if the file can not be created.
        /// </summary>
        TextWriter? GetDebugFileWriter();
    }
}
