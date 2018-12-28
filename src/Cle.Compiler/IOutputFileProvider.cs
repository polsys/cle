using System.IO;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// Provides methods for creating output file streams.
    /// The lifetime is handled by the creator.
    /// </summary>
    public interface IOutputFileProvider
    {
        /// <summary>
        /// Gets a debug log writer.
        /// May return null if the file can not be created.
        /// </summary>
        [CanBeNull]
        TextWriter GetDebugFileWriter();
    }
}
