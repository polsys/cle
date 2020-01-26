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

        public override bool Equals(object? obj)
        {
            // This is mostly used for simplifying the test code
            if (obj is string objAsString)
            {
                return Equals(objAsString);
            }

            return Equals(obj as TypeSyntax);
        }

        public abstract override int GetHashCode();

        public abstract bool Equals(TypeSyntax? other);

        public abstract bool Equals(string? other);
        
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
