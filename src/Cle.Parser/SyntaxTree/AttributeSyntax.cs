using System.Collections.Immutable;
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

        /// <summary>
        /// Gets the parameter list of the attribute.
        /// May be empty.
        /// </summary>
        public ImmutableList<LiteralSyntax> Parameters { get; }

        public AttributeSyntax(
            string name,
            ImmutableList<LiteralSyntax> parameters,
            TextPosition position)
            : base(position)
        {
            Name = name;
            Parameters = parameters;
        }
    }
}
