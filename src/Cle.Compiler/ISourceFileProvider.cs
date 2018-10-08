using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// The compiler uses this interface to get source file contents to parse.
    /// </summary>
    public interface ISourceFileProvider
    {
        /// <summary>
        /// Tries to return a sequence of source file names that compose the specified module.
        /// If the module could not be found, returns false.
        /// </summary>
        bool TryGetFilenamesForModule([NotNull] string moduleName, [CanBeNull] out IEnumerable<string> filenames);

        /// <summary>
        /// Tries to open the specified source file and return its contents as a view of bytes.
        /// Returns false if the file could not be read.
        /// This function may be called concurrently for different files.
        /// </summary>
        /// <param name="filename">The file to open, originated from <see cref="TryGetFilenamesForModule"/>.</param>
        /// <param name="fileBytes">View of full file bytes.</param>
        bool TryGetSourceFile([NotNull] string filename, out Memory<byte> fileBytes);
    }
}
