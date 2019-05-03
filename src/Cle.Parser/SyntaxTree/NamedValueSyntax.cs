using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a variable or constant reference.
    /// </summary>
    public sealed class NamedValueSyntax : ExpressionSyntax
    {
        /// <summary>
        /// Gets the referenced variable/constant name.
        /// </summary>
        public string Name { get; }

        public NamedValueSyntax(string name, TextPosition position)
            : base(position)
        {
            Name = name;
        }
    }
}
