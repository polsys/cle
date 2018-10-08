namespace Cle.Compiler
{
    /// <summary>
    /// Additional options for the compiler.
    /// </summary>
    public class CompilationOptions
    {
        /// <summary>
        /// If true, the compiler should dump debug logging.
        /// Some of this debug logging may be only enabled in Debug builds of the compiler.
        /// </summary>
        public bool DebugOutput { get; }

        public CompilationOptions(bool debugOutput = false)
        {
            DebugOutput = debugOutput;
        }
    }
}
