using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Base class for syntax tree nodes for expressions.
    /// </summary>
    public abstract class ExpressionSyntax : SyntaxNode
    {
        protected ExpressionSyntax(TextPosition position) : base(position)
        {
        }
    }
}
