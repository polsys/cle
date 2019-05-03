using System.Collections.Immutable;
using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a function (either global or within class) definition.
    /// </summary>
    public sealed class FunctionSyntax : SyntaxNode
    {
        /// <summary>
        /// Gets the simple name of this function.
        /// The name is guaranteed to be syntactically valid, but it may be semantically invalid.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the visibility class of this function.
        /// </summary>
        public Visibility Visibility { get; }

        /// <summary>
        /// Gets the declared return type of this function.
        /// The type name may be full or simple and the type might not exist.
        /// </summary>
        public string ReturnTypeName { get; }

        /// <summary>
        /// Gets the parameter list of this function.
        /// </summary>
        public ImmutableList<ParameterDeclarationSyntax> Parameters { get; }

        /// <summary>
        /// Gets the list of attributes applied to this function.
        /// The list may be empty.
        /// </summary>
        public ImmutableList<AttributeSyntax> Attributes { get; }

        /// <summary>
        /// Gets the code block of this function.
        /// </summary>
        public BlockSyntax Block { get; }

        public FunctionSyntax(
            string name,
            string returnType,
            Visibility visibility,
            ImmutableList<ParameterDeclarationSyntax> parameters,
            ImmutableList<AttributeSyntax> attributes,
            BlockSyntax block,
            TextPosition position)
            : base(position)
        {
            Name = name;
            ReturnTypeName = returnType;
            Visibility = visibility;
            Parameters = parameters;
            Attributes = attributes;
            Block = block;
        }
    }
}
