using System.Collections.Generic;
using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// Diagnostics and statistics for a compilation.
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Gets the total number of modules included in the compilation.
        /// </summary>
        public int ModuleCount { get; }

        /// <summary>
        /// Gets the number of modules successfully compiled.
        /// </summary>
        public int SucceededCount { get; }

        /// <summary>
        /// Gets the number of modules with errors.
        /// This number does not include modules skipped because of failed dependencies.
        /// </summary>
        public int FailedCount { get; }

        /// <summary>
        /// Gets the diagnostic messages produced by this compilation.
        /// The order of diagnostics is unspecified.
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        internal CompilationResult(int moduleCount, int succeededCount, int failedCount,
            [NotNull, ItemNotNull] IReadOnlyList<Diagnostic> diagnostics)
        {
            ModuleCount = moduleCount;
            SucceededCount = succeededCount;
            FailedCount = failedCount;
            Diagnostics = diagnostics;
        }
    }
}
