using System;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// Flags for local values.
    /// Not all flags are set by <see cref="MethodCompiler"/>.
    /// </summary>
    [Flags]
    public enum LocalFlags
    {
        None = 0,
        /// <summary>
        /// This local is a parameter and must not be optimized away.
        /// </summary>
        Parameter = 1,
    }
}
