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
        public string? DebugPattern { get; }

        /// <summary>
        /// If true, an assembly language listing for the compiled program should be produced along the executable.
        /// </summary>
        public bool EmitDisassembly { get; }

        /// <summary>
        /// Gets the name of the main module.
        /// </summary>
        public string MainModule { get; }

        public CompilationOptions(string mainModule, string? debugPattern = null, bool emitDisassembly = false)
        {
            MainModule = mainModule;
            DebugPattern = debugPattern;
            EmitDisassembly = emitDisassembly;
        }
    }
}
