using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for an unsigned integer literal.
    /// </summary>
    public sealed class IntegerLiteralSyntax : LiteralSyntax
    {
        /// <summary>
        /// Gets the numeric value of this literal.
        /// </summary>
        public ulong Value { get; }

        public IntegerLiteralSyntax(ulong value, TextPosition position)
            : base(position)
        {
            Value = value;
        }
    }
}
