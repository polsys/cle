using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// Additional options for the compiler.
    /// </summary>
    public class CompilationOptions
    {
        /// <summary>
        /// If true, the compiler should dump debug logging for methods matching <see cref="DebugPattern"/>.
        /// Some of this debug logging may be only enabled in Debug builds of the compiler.
        /// </summary>
        public bool DebugOutput => DebugPattern != null;

        /// <summary>
        /// Gets a regex pattern the will be used for matching method names to dump debug logging for.
        /// </summary>
        [CanBeNull]
        public string DebugPattern { get; }

        /// <summary>
        /// Gets the name of the main module.
        /// </summary>
        [NotNull]
        public string MainModule { get; }

        public CompilationOptions([NotNull] string mainModule, string debugPattern = null)
        {
            MainModule = mainModule;
            DebugPattern = debugPattern;
        }
    }
}
