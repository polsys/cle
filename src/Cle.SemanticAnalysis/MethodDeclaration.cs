using System.Collections.Immutable;
using Cle.Common;
using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Type information for a method.
    /// See implementing classes <see cref="NativeMethodDeclaration"/> and <see cref="ImportedMethodDeclaration"/>.
    /// </summary>
    public abstract class MethodDeclaration
    {
        /// <summary>
        /// Gets the internal index for referring to the method in the Intermediate Representation bytecode.
        /// This also applies to methods that are imported instead of compiled.
        /// </summary>
        public int BodyIndex { get; }

        /// <summary>
        /// Gets the return type of this method.
        /// </summary>
        public TypeDefinition ReturnType { get; }

        /// <summary>
        /// Gets the types of the parameters to this method.
        /// </summary>
        public ImmutableList<TypeDefinition> ParameterTypes { get; }

        /// <summary>
        /// Gets the visibility class of this method.
        /// </summary>
        public Visibility Visibility { get; }

        /// <summary>
        /// Gets the full name of this method.
        /// This is only used for debugging purposes.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the name of the file this method is defined in.
        /// This is used for visibility resolution of private methods.
        /// </summary>
        public string DefiningFilename { get; }

        /// <summary>
        /// Gets the source code position where the definition of this function starts.
        /// </summary>
        public TextPosition DefinitionPosition { get; }

        protected MethodDeclaration(
            int bodyIndex,
            TypeDefinition returnType, 
            ImmutableList<TypeDefinition> parameterTypes,
            Visibility visibility,
            string fullName,
            string definingFilename,
            TextPosition sourcePosition)
        {
            BodyIndex = bodyIndex;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            Visibility = visibility;
            FullName = fullName;
            DefiningFilename = definingFilename;
            DefinitionPosition = sourcePosition;
        }
    }
}
