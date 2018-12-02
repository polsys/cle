using Cle.Common;
using JetBrains.Annotations;

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
        [NotNull]
        public string Name { get; }

        public NamedValueSyntax([NotNull] string name, TextPosition position)
            : base(position)
        {
            Name = name;
        }
    }
}
