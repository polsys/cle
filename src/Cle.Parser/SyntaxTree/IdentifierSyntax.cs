using System;
using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a variable, method or constant reference.
    /// </summary>
    public sealed class IdentifierSyntax : ExpressionSyntax, IEquatable<string>
    {
        /// <summary>
        /// Gets the referenced variable/constant name.
        /// </summary>
        public string Name { get; }

        public IdentifierSyntax(string name, TextPosition position)
            : base(position)
        {
            Name = name;
        }

        public bool Equals(string other)
        {
            // This is mostly for simplifying the test code
            return Name == other;
        }
    }
}
