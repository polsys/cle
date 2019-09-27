namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// Implementation types of method calls.
    /// </summary>
    public enum MethodCallType
    {
        Invalid,
        /// <summary>
        /// The call target is defined in Clé code.
        /// </summary>
        Native,
        /// <summary>
        /// The call target is defined in an external library loaded at runtime.
        /// </summary>
        Imported
    }
}
