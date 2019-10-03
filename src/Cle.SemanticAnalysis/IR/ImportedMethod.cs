namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// The method body for an imported method.
    /// Contains the same information as <see cref="ImportedMethodDeclaration"/>,
    /// but accessible in the body array instead of via declaration resolution.
    /// </summary>
    public sealed class ImportedMethod : MethodBody
    {
        /// <summary>
        /// Gets the name of this method in the imported library.
        /// The name is an array of ASCII characters.
        /// </summary>
        public byte[] ImportName { get; }

        /// <summary>
        /// Gets the name of the library where this method is defined.
        /// The name is an array of ASCII characters.
        /// </summary>
        public byte[] ImportLibrary { get; }

        public ImportedMethod(string fullName, byte[] importName, byte[] importLibrary) 
            : base(fullName)
        {
            ImportName = importName;
            ImportLibrary = importLibrary;
        }
    }
}
