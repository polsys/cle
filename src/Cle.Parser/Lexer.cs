using System;
using System.Collections.Generic;
using System.Text;
using Cle.Common;

namespace Cle.Parser
{
    /// <summary>
    /// This class splits UTF-8 encoded source code input into tokens.
    /// </summary>
    internal class Lexer
    {
        private readonly Memory<byte> _source;

        private int _currentOffsetBytes;
        private int _currentTokenLengthBytes;
        private int _currentRow = 1;
        private int _currentByteInRow;

        /// <summary>
        /// Lookup table for keywords and two-character symbols.
        /// </summary>
        private static readonly List<(byte[], TokenType)> s_keywords = InitializeKeywords();

        /// <summary>
        /// Lookup table for single-character symbols.
        /// This does not include " or classification for any other character classes.
        /// </summary>
        private static readonly byte[] s_symbols = InitializeSymbolTable();

        public Lexer(Memory<byte> source)
        {
            _source = source;
            
            // Read the first token so that PeekToken works
            SkipByteOrderMark();
            AdvanceToNextToken();
        }

        /// <summary>
        /// Gets the position of the currently pending token.
        /// </summary>
        public TextPosition Position => new TextPosition(_currentOffsetBytes, _currentRow, _currentByteInRow);

        /// <summary>
        /// Gets the position of the last token returned by <see cref="GetToken"/>.
        /// </summary>
        public TextPosition LastPosition { get; private set; }

        /// <summary>
        /// Gets the next token and advances the lexer position.
        /// </summary>
        public ReadOnlySpan<byte> GetToken()
        {
            var result = PeekToken();
            LastPosition = Position;
            AdvanceToNextToken();

            return result;
        }

        /// <summary>
        /// Gets the type of the next token and advances the lexer position.
        /// </summary>
        public TokenType GetTokenType()
        {
            return ClassifyToken(GetToken());
        }

        /// <summary>
        /// Gets the next token but does not advance the lexer position.
        /// </summary>
        public ReadOnlySpan<byte> PeekToken()
        {
            return _source.Slice(_currentOffsetBytes, _currentTokenLengthBytes).Span;
        }

        /// <summary>
        /// Gets the type of the next token but does not advance the lexer position.
        /// </summary>
        public TokenType PeekTokenType()
        {
            return ClassifyToken(PeekToken());
        }

        private void AdvanceToNextToken()
        {
            // Move past the current token
            _currentOffsetBytes += _currentTokenLengthBytes;
            _currentByteInRow += _currentTokenLengthBytes;
            _currentTokenLengthBytes = 0;

            // Skip whitespace and comments until we hit a token
            while (_currentOffsetBytes < _source.Length)
            {
                var positionBeforeSkipping = _currentOffsetBytes;

                SkipWhitespace();
                SkipSingleLineComment();

                if (positionBeforeSkipping == _currentOffsetBytes)
                {
                    break;
                }
            }

            // Do not go past the end of the file
            if (_currentOffsetBytes >= _source.Length)
                return;

            // Find the end of the new token
            if (_source.Slice(_currentOffsetBytes, 1).Span[0] == (byte)'"')
            {
                // Within a string literal, read until the ending quotation mark or a linefeed is found
                // However, ignore escaped (\") quotes
                // " is considered a part of the token
                _currentTokenLengthBytes = 1;
                var previousByte = (byte)0;

                while (TryReadByte(out var currentByte))
                {
                    if (currentByte == (byte)'\n')
                    {
                        break;
                    }
                    else if (currentByte == (byte)'"' && previousByte != (byte)'\\')
                    {
                        _currentTokenLengthBytes++;
                        break;
                    }
                    else
                    {
                        _currentTokenLengthBytes++;
                        previousByte = currentByte;
                    }
                }
            }
            else
            {
                // Else, read until whitespace or end of file
                _currentTokenLengthBytes = 0;
                while (TryReadByte(out var currentByte))
                {
                    var isSymbol = IsSymbol(currentByte);

                    // Read until whitespace or a symbol ends the current token
                    if (IsWhitespace(currentByte) || (isSymbol && _currentTokenLengthBytes > 0))
                        break;

                    _currentTokenLengthBytes++;

                    // If this token is a symbol, break after reading one (or two in case of longer symbols) character
                    if (isSymbol)
                    {
                        // Two-char symbols
                        if (TryReadByte(out var nextByte))
                        {
                            switch (nextByte)
                            {
                                case (byte)'=' when currentByte == (byte)'=':
                                case (byte)'=' when currentByte == (byte)'!':
                                case (byte)'<' when currentByte == (byte)'<':
                                case (byte)'>' when currentByte == (byte)'>':
                                case (byte)'=' when currentByte == (byte)'<':
                                case (byte)'=' when currentByte == (byte)'>':
                                case (byte)'&' when currentByte == (byte)'&':
                                case (byte)'|' when currentByte == (byte)'|':
                                    _currentTokenLengthBytes++;
                                    break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next byte but does not update current position.
        /// Returns false if end of file is reached.
        /// </summary>
        /// <param name="b">Will contain the byte read, or undefined if EOF.</param>
        private bool TryReadByte(out byte b)
        {
            // If EOF, return false
            if (_currentOffsetBytes + _currentTokenLengthBytes >= _source.Length)
            {
                b = 0;
                return false;
            }

            // Read the byte
            b = _source.Slice(_currentOffsetBytes + _currentTokenLengthBytes, 1).Span[0];
            return true;
        }

        private void SkipByteOrderMark()
        {
            if (_currentOffsetBytes != 0)
                throw new InvalidOperationException("BOM can only be skipped at start of source text");

            if (_source.Length < 3)
                return;

            if (_source.Span[0] == 0xEF && _source.Span[1] == 0xBB && _source.Span[2] == 0xBF)
            {
                _currentOffsetBytes = 3;
            }
        }

        private void SkipWhitespace()
        {
            while (TryReadByte(out var b) && IsWhitespace(b))
            {
                _currentOffsetBytes++;

                // Update the position
                if (b == (byte)'\n')
                {
                    _currentRow++;
                    _currentByteInRow = 0;
                }
                else
                {
                    _currentByteInRow++;
                }
            }
        }

        private void SkipSingleLineComment()
        {
            if (_source.Length - _currentOffsetBytes >= 2)
            {
                var possibleDoubleSlash = _source.Slice(_currentOffsetBytes, 2).Span;

                if (possibleDoubleSlash[0] == (byte)'/' && possibleDoubleSlash[1] == (byte)'/')
                {
                    // Read until end-of-line
                    // SkipWhitespace will eat the newline character and update the row
                    while (TryReadByte(out var ch) && ch != (byte)'\n')
                    {
                        _currentOffsetBytes++;
                        _currentByteInRow++;
                    }
                }
            }
        }

        private static bool IsWhitespace(byte character)
        {
            return character == (byte)' '
                   || character == (byte)'\n'
                   || character == (byte)'\r'
                   || character == (byte)'\t';
        }

        private static bool IsSymbol(byte character)
        {
            return s_symbols[character] != 0;
        }

        private static TokenType ClassifyToken(ReadOnlySpan<byte> token)
        {
            if (token.IsEmpty)
                return TokenType.EndOfFile;

            // Early out for string literals
            if (token[0] == (byte)'"')
                return TokenType.StringLiteral;

            // Early out for numbers, as identifiers may not begin with digits
            // If the token is not a valid number, the parser will do the complaining
            if (token[0] >= (byte)'0' && token[0] <= (byte)'9')
                return TokenType.Number;

            // TODO: Early out for tokens beginning with _ if that is beneficial in real workloads

            // Test for one-character symbols
            if (token.Length == 1)
            {
                var symbolType = s_symbols[token[0]];
                if (symbolType != 0)
                    return (TokenType)symbolType;
            }

            // Keywords and two-character symbols are collected in a list of (keyword bytes, token type) tuples
            foreach (var (bytes, tokenType) in s_keywords)
            {
                if (token.SequenceEqual(bytes.AsSpan()))
                    return tokenType;
            }

            // If the token did not match any of the previous, it is an identifier
            return TokenType.Identifier;
        }

        private static List<(byte[], TokenType)> InitializeKeywords()
        {
            // TODO: As the list is traversed in order, the most common tokens could be put first.
            return new List<(byte[], TokenType)>
            {
                (Encoding.UTF8.GetBytes("=="), TokenType.DoubleEquals),
                (Encoding.UTF8.GetBytes("!="), TokenType.NotEquals),
                (Encoding.UTF8.GetBytes("<="), TokenType.LessThanOrEquals),
                (Encoding.UTF8.GetBytes(">="), TokenType.GreaterThanOrEquals),
                (Encoding.UTF8.GetBytes("<<"), TokenType.DoubleLessThan),
                (Encoding.UTF8.GetBytes(">>"), TokenType.DoubleGreaterThan),
                (Encoding.UTF8.GetBytes("&&"), TokenType.DoubleAmpersand),
                (Encoding.UTF8.GetBytes("||"), TokenType.DoubleBar),
                (Encoding.UTF8.GetBytes("else"), TokenType.Else),
                (Encoding.UTF8.GetBytes("false"), TokenType.False),
                (Encoding.UTF8.GetBytes("if"), TokenType.If),
                (Encoding.UTF8.GetBytes("internal"), TokenType.Internal),
                (Encoding.UTF8.GetBytes("namespace"), TokenType.Namespace),
                (Encoding.UTF8.GetBytes("private"), TokenType.Private),
                (Encoding.UTF8.GetBytes("public"), TokenType.Public),
                (Encoding.UTF8.GetBytes("return"), TokenType.Return),
                (Encoding.UTF8.GetBytes("true"), TokenType.True),
                (Encoding.UTF8.GetBytes("var"), TokenType.Var),
                (Encoding.UTF8.GetBytes("while"), TokenType.While),
            };
        }

        private static byte[] InitializeSymbolTable()
        {
            var table = new byte[256];
            table[(byte)'+'] = (byte)TokenType.Plus;
            table[(byte)'-'] = (byte)TokenType.Minus;
            table[(byte)'*'] = (byte)TokenType.Asterisk;
            table[(byte)'/'] = (byte)TokenType.ForwardSlash;
            table[(byte)'%'] = (byte)TokenType.Percent;
            table[(byte)'='] = (byte)TokenType.Equals;
            table[(byte)'<'] = (byte)TokenType.LessThan;
            table[(byte)'>'] = (byte)TokenType.GreaterThan;
            table[(byte)'!'] = (byte)TokenType.Exclamation;
            table[(byte)'~'] = (byte)TokenType.Tilde;
            table[(byte)'^'] = (byte)TokenType.Circumflex;
            table[(byte)'&'] = (byte)TokenType.Ampersand;
            table[(byte)'|'] = (byte)TokenType.Bar;
            table[(byte)';'] = (byte)TokenType.Semicolon;
            table[(byte)','] = (byte)TokenType.Comma;
            table[(byte)'('] = (byte)TokenType.OpenParen;
            table[(byte)')'] = (byte)TokenType.CloseParen;
            table[(byte)'{'] = (byte)TokenType.OpenBrace;
            table[(byte)'}'] = (byte)TokenType.CloseBrace;
            table[(byte)'['] = (byte)TokenType.OpenBracket;
            table[(byte)']'] = (byte)TokenType.CloseBracket;

            return table;
        }
    }
}
