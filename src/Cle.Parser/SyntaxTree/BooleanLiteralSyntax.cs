using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a Boolean literal.
    /// </summary>
    public sealed class BooleanLiteralSyntax : LiteralSyntax
    {
        /// <summary>
        /// Gets the Boolean value of this literal.
        /// </summary>
        public bool Value { get; }

        public BooleanLiteralSyntax(bool value, TextPosition position)
            : base(position)
        {
            Value = value;
        }
    }
}
