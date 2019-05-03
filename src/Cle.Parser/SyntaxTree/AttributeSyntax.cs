using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for an attribute applied to a function.
    /// </summary>
    public sealed class AttributeSyntax : SyntaxNode
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        public AttributeSyntax(
            string name,
            TextPosition position)
            : base(position)
        {
            Name = name;
        }
    }
}
