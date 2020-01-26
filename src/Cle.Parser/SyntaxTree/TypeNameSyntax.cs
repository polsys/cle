using System;
using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a named type (built-in or user-defined).
    /// </summary>
    public sealed class TypeNameSyntax : TypeSyntax
    {
        /// <summary>
        /// Gets the type name.
        /// This may reference a built-in or a user-defined type, with either a simple or full name.
        /// </summary>
        public string TypeName { get; }

        public TypeNameSyntax(string typeName, TextPosition position)
            : base(position)
        {
            TypeName = typeName;
        }

        public override bool Equals(TypeSyntax? other)
        {
            if (other is TypeNameSyntax name)
            {
                return TypeName == name.TypeName;
            }

            return false;
        }

        public override bool Equals(string? other)
        {
            return TypeName == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TypeName);
        }

        public override string ToString()
        {
            return TypeName;
        }
    }
}
