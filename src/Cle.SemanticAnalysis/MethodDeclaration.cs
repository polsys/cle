using Cle.Common;
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
        /// Gets the index associated with the compiled method body.
        /// </summary>
        public int BodyIndex { get; }

        /// <summary>
        /// If true, this method is the entry point for an executable application.
        /// </summary>
        public bool IsEntryPoint { get; }

        /// <summary>
        /// Gets the return type of this method.
        /// </summary>
        [NotNull]
        public TypeDefinition ReturnType { get; }

        /// <summary>
        /// Gets the visibility class of this method.
        /// </summary>
        public Visibility Visibility { get; }

        /// <summary>
        /// Gets the name of the file this method is defined in.
        /// This is used for visibility resolution of private methods.
        /// </summary>
        [NotNull]
        public string DefiningFilename { get; }

        /// <summary>
        /// Gets the source code position where the definition of this function starts.
        /// </summary>
        public TextPosition DefinitionPosition { get; }

        public MethodDeclaration(
            int bodyIndex,
            [NotNull] TypeDefinition returnType, 
            Visibility visibility,
            [NotNull] string definingFilename,
            TextPosition sourcePosition,
            bool isEntryPoint)
        {
            BodyIndex = bodyIndex;
            ReturnType = returnType;
            Visibility = visibility;
            DefiningFilename = definingFilename;
            DefinitionPosition = sourcePosition;
            IsEntryPoint = isEntryPoint;
        }
    }
}
