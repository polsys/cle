namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// A base class for method bodies.
    /// There are two principal types of methods: <see cref="CompiledMethod"/> and <see cref="ImportedMethod"/>.
    /// </summary>
    public abstract class MethodBody
    {
        /// <summary>
        /// Gets the full name, as referenced in Clé code, of this method.
        /// This can be used for debugging and emitting symbols.
        /// </summary>
        public string FullName { get; }

        protected MethodBody(string fullName)
        {
            FullName = fullName;
        }
    }
}
