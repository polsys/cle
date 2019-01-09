using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a function parameter declaration.
    /// </summary>
    public sealed class ParameterDeclarationSyntax : SyntaxNode
    {
        /// <summary>
        /// Gets the declared type of the parameter.
        /// </summary>
        [NotNull]
        public string TypeName { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        [NotNull]
        public string Name { get; }

        public ParameterDeclarationSyntax(
            [NotNull] string typeName,
            [NotNull] string name,
            TextPosition position)
            : base(position)
        {
            TypeName = typeName;
            Name = name;
        }
    }
}
