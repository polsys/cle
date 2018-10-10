using Cle.Common;
using JetBrains.Annotations;

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
        [NotNull]
        public string Name { get; }

        /// <summary>
        /// Gets the visibility class of this function.
        /// </summary>
        public Visibility Visibility { get; }

        /// <summary>
        /// Gets the declared return type of this function.
        /// The type name may be full or simple and the type might not exist.
        /// </summary>
        [NotNull]
        public string ReturnTypeName { get; }

        /// <summary>
        /// Gets the code block of this function.
        /// </summary>
        [NotNull]
        public BlockSyntax Block { get; }

        public FunctionSyntax(
            [NotNull] string name,
            [NotNull] string returnType,
            Visibility visibility,
            [NotNull] BlockSyntax block,
            TextPosition position)
            : base(position)
        {
            Name = name;
            ReturnTypeName = returnType;
            Visibility = visibility;
            Block = block;
        }
    }
}
