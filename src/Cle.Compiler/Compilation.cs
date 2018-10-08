using System.Collections.Generic;
using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// The class that holds all type and method information within a compilation session.
    /// A single instance of this type may be used concurrently from multiple threads.
    /// This class handles synchronization unless otherwise noted.
    /// </summary>
    internal class Compilation
    {
        /// <summary>
        /// Gets the list of diagnostics associated with this compilation.
        /// <see cref="DiagnosticsLock"/> must be acquired prior to accessing this property.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        [NotNull]
        [ItemNotNull]
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        /// <summary>
        /// Synchronization object for <see cref="Diagnostics"/>.
        /// </summary>
        [NotNull]
        public object DiagnosticsLock { get; } = new object();

        /// <summary>
        /// Gets whether any diagnostics are classified as errors.
        /// This value may be updated concurrently in sync with <see cref="Diagnostics"/>.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Adds the given collection of diagnostics to <see cref="Diagnostics"/>.
        /// This function may be called from multiple threads.
        /// </summary>
        public void AddDiagnostics([NotNull] IReadOnlyList<Diagnostic> diagnosticsToAdd)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.AddRange(diagnosticsToAdd);

                // Update HasErrors
                foreach (var diagnostic in diagnosticsToAdd)
                {
                    if (diagnostic.IsError)
                        HasErrors = true;
                }
            }
        }

        /// <summary>
        /// Adds an error about missing source file.
        /// This function may be called from multiple threads.
        /// </summary>
        /// <param name="moduleName">The module that should contain the file.</param>
        /// <param name="filename">The name of the missing file.</param>
        public void AddMissingFileError([NotNull] string moduleName, [NotNull] string filename)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.Add(new Diagnostic(DiagnosticCode.SourceFileNotFound, default, filename, moduleName, null));
                HasErrors = true;
            }
        }

        /// <summary>
        /// Adds an error about missing module.
        /// This function may be called from multiple threads.
        /// </summary>
        /// <param name="moduleName">The module that was not found.</param>
        public void AddMissingModuleError([NotNull] string moduleName)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.Add(new Diagnostic(DiagnosticCode.ModuleNotFound, default, null, moduleName, null));
                HasErrors = true;
            }
        }
    }
}
