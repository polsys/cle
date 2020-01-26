using Cle.Common;

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
        public TypeSyntax Type { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        public ParameterDeclarationSyntax(
            TypeSyntax type,
            string name,
            TextPosition position)
            : base(position)
        {
            Type = type;
            Name = name;
        }
    }
}
