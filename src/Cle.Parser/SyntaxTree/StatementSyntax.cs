using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Base class for syntax tree nodes for statements.
    /// </summary>
    public abstract class StatementSyntax : SyntaxNode
    {
        protected StatementSyntax(TextPosition position) : base(position)
        {
        }
    }
}
