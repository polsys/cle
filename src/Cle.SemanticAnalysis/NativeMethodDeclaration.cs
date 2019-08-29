using System.Collections.Immutable;
using Cle.Common;
using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Type information for a method implemented in the source code under compilation.
    /// </summary>
    public sealed class NativeMethodDeclaration : MethodDeclaration
    {
        /// <summary>
        /// If true, this method is the entry point for an executable application.
        /// </summary>
        public bool IsEntryPoint { get; }

        public NativeMethodDeclaration(
            int bodyIndex,
            TypeDefinition returnType,
            ImmutableList<TypeDefinition> parameterTypes,
            Visibility visibility,
            string fullName,
            string definingFilename,
            TextPosition sourcePosition,
            bool isEntryPoint)
            : base(bodyIndex, returnType, parameterTypes, visibility, fullName, definingFilename, sourcePosition)
        {
            IsEntryPoint = isEntryPoint;
        }
    }
}
