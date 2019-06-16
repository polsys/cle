using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a string literal.
    /// </summary>
    public sealed class StringLiteralSyntax : LiteralSyntax
    {
        /// <summary>
        /// Gets the UTF-8 encoded bytes of this string.
        /// This does not contain the surrounding quotes, and all escape sequences are processed.
        /// </summary>
        public byte[] Value { get; }

        public StringLiteralSyntax(byte[] escapedStringContents, TextPosition position)
            : base(position)
        {
            Value = escapedStringContents;
        }
    }
}
