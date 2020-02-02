using System;
using System.Collections.Generic;
using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Base class for type name syntax tree nodes.
    /// </summary>
    public abstract class TypeSyntax : SyntaxNode, IEquatable<TypeSyntax>, IEquatable<string>
    {
        protected TypeSyntax(TextPosition position)
            : base(position)
        {
        }

        public abstract override bool Equals(object? obj);

        public abstract bool Equals(TypeSyntax? other);

        /// <summary>
        /// For simplifying test code.
        /// </summary>
        public abstract bool Equals(string? other);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public static bool operator ==(TypeSyntax left, TypeSyntax right)
        {
            return EqualityComparer<TypeSyntax>.Default.Equals(left, right);
        }

        public static bool operator !=(TypeSyntax left, TypeSyntax right)
        {
            return !(left == right);
        }
    }
}
