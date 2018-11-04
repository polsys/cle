namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Represents a method that has passed semantic analysis and can be emitted.
    /// Instances are mutable and can be transformed via optimizations.
    /// </summary>
    public class CompiledMethod
    {
        /// <summary>
        /// Gets or sets the basic block graph for this method.
        /// </summary>
        public BasicBlockGraph Body { get; set; }
    }
}
