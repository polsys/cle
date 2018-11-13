using Cle.Common.TypeSystem;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Type information for a method.
    /// </summary>
    public class MethodDeclaration
    {
        /// <summary>
        /// Gets the return type of this method.
        /// </summary>
        [NotNull]
        public TypeDefinition ReturnType { get; }

        public MethodDeclaration([NotNull] TypeDefinition returnType)
        {
            ReturnType = returnType;
        }
    }
}
