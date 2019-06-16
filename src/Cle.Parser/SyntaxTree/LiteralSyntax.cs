using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Special case of expression syntax, the base class for all literals.
    /// </summary>
    public abstract class LiteralSyntax : ExpressionSyntax
    {
        public LiteralSyntax(TextPosition position)
            : base(position)
        {
        }
    }
}
