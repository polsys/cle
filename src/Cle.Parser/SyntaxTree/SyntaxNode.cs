using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Base class for syntax tree nodes.
    /// </summary>
    public abstract class SyntaxNode
    {
        public TextPosition Position { get; }

        protected SyntaxNode(TextPosition position)
        {
            Position = position;
        }
    }
}
