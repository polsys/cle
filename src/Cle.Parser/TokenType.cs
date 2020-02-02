namespace Cle.Parser
{
    /// <summary>
    /// Classification of a token.
    /// A token is either an identifier, a literal, a keyword or a symbol.
    /// End-of-file is also classified as a special token.
    /// </summary>
    public enum TokenType
    {
        // Other
        Unknown,
        Identifier,
        StringLiteral,
        Number,

        // Keywords
        Else,
        False,
        If,
        Internal,
        Namespace,
        Private,
        Public,
        Return,
        True,
        Var,
        While,

        // Symbols
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Percent,
        Equals,
        DoubleEquals,
        NotEquals,
        LessThan,
        LessThanOrEquals,
        DoubleLessThan,
        GreaterThan,
        GreaterThanOrEquals,
        DoubleGreaterThan,
        Exclamation,
        Tilde,
        Circumflex,
        Ampersand,
        DoubleAmpersand,
        Bar,
        DoubleBar,
        Semicolon,
        Comma,
        OpenParen,
        CloseParen,
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        EndOfFile,
    }
}
