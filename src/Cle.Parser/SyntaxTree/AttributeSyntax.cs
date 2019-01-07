using Cle.Common;
using JetBrains.Annotations;

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
        [NotNull]
        public string Name { get; }

        public AttributeSyntax(
            [NotNull] string name,
            TextPosition position)
            : base(position)
        {
            Name = name;
        }
    }
}
