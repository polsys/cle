using System;

namespace Cle.Common.TypeSystem
{
    /// <summary>
    /// The base class for Cle types, both built-in and user-defined ones.
    /// Each type is defined exactly once and the <see cref="Equals(TypeDefinition)"/> method
    /// can be used for comparing types.
    /// </summary>
    public abstract class TypeDefinition : IEquatable<TypeDefinition>
    {
        /// <summary>
        /// Gets the fully qualified name of this type.
        /// </summary>
        public abstract string TypeName { get; }

        public abstract bool Equals(TypeDefinition other);
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }
}
