using System.Collections.Immutable;
using Cle.Common;
using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Type information for a method imported from an external library.
    /// </summary>
    public sealed class ImportedMethodDeclaration : MethodDeclaration
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

        public ImportedMethodDeclaration(
            int bodyIndex,
            TypeDefinition returnType,
            ImmutableList<TypeDefinition> parameterTypes,
            Visibility visibility,
            string fullName,
            string definingFilename,
            TextPosition sourcePosition,
            byte[] importName,
            byte[] importLibrary)
            : base(bodyIndex, returnType, parameterTypes, visibility, fullName, definingFilename, sourcePosition)
        {
            ImportName = importName;
            ImportLibrary = importLibrary;
        }
    }
}
