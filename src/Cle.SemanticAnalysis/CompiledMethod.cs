using System;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Represents a method that has passed semantic analysis.
    /// Instances can be transformed via optimizations and passed to code generation.
    /// </summary>
    public class CompiledMethod
    {
        /// <summary>
        /// Gets the basic block graph for this method.
        /// </summary>
        public BasicBlockGraph Body { get; }
    }
}
