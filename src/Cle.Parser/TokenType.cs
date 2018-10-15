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
        Internal,
        Namespace,
        Private,
        Public,
        Return,

        // Symbols
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        Semicolon,
        OpenParen,
        CloseParen,
        OpenBrace,
        CloseBrace,
        EndOfFile,
    }
}
