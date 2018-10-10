using System;
using System.Text;
using Cle.Common;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Parser.UnitTests
{
    public class LexerTests
    {
        [Test]
        public void GetToken_gets_tokens_separated_by_whitespace_until_end_of_file()
        {
            var source = StringToMemory("one two\nthree\tfour\r\nfive   six");
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("one"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("two"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("three"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("four"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("five"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("six"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.Empty);
        }

        [Test]
        public void GetToken_initial_end_of_file()
        {
            var source = StringToMemory("");
            var lexer = new Lexer(source);

            Assert.That(lexer.GetToken().Length, Is.Zero);
        }

        [Test]
        public void GetToken_does_not_go_past_end_of_file()
        {
            var source = StringToMemory("token");
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("token"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.Empty);
            Assert.That(Utf8ToString(lexer.GetToken()), Is.Empty);
        }

        [Test]
        public void GetToken_reads_string_literal_as_whole()
        {
            const string literal = "\"A longer string with sp\x00E9cial characters.\"";
            var source = StringToMemory(literal);
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo(literal));
        }

        [Test]
        public void GetToken_reads_string_literal_only_until_linefeed()
        {
            const string literal = "\"Something\nsomething";
            var source = StringToMemory(literal);
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("\"Something"));
        }

        [Test]
        public void GetToken_ignores_escaped_quote_in_literal()
        {
            const string literal = "\"\\\"\"";
            var source = StringToMemory(literal);
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("\"\\\"\""));
        }

        [Test]
        public void GetTokenType_classifies_tokens_correctly()
        {
            var source = StringToMemory("namespace NS; \"literal\"(){name 123}");
            var lexer = new Lexer(source);

            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.Namespace));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.Identifier));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.Semicolon));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.StringLiteral));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.OpenParen));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.CloseParen));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.OpenBrace));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.Identifier));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.Number));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.CloseBrace));
            Assert.That(lexer.GetTokenType(), Is.EqualTo(TokenType.EndOfFile));
        }

        [Test]
        public void LastPosition_is_updated()
        {
            var source = StringToMemory(" one two");
            var lexer = new Lexer(source);

            var position1 = lexer.Position;
            lexer.GetToken();
            Assert.That(lexer.LastPosition, Is.EqualTo(position1));

            var position2 = lexer.Position;
            lexer.GetToken();
            Assert.That(lexer.LastPosition, Is.EqualTo(position2));
        }

        [Test]
        public void PeekToken_does_not_advance_position()
        {
            var source = StringToMemory("one two");
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.PeekToken()), Is.EqualTo("one"));
            Assert.That(Utf8ToString(lexer.PeekToken()), Is.EqualTo("one"));
        }

        [Test]
        public void PeekToken_initial_end_of_file()
        {
            var source = StringToMemory("");
            var lexer = new Lexer(source);

            Assert.That(lexer.PeekToken().Length, Is.Zero);
        }

        [Test]
        public void PeekToken_only_whitespace()
        {
            var source = StringToMemory("   \t");
            var lexer = new Lexer(source);

            Assert.That(lexer.PeekToken().Length, Is.Zero);
        }

        [Test]
        public void PeekTokenType_classifies_tokens_correctly()
        {
            var source = StringToMemory("namespace Namespace");
            var lexer = new Lexer(source);

            Assert.That(lexer.PeekTokenType(), Is.EqualTo(TokenType.Namespace));
            lexer.GetToken();
            Assert.That(lexer.PeekTokenType(), Is.EqualTo(TokenType.Identifier));
            lexer.GetToken();
            Assert.That(lexer.PeekTokenType(), Is.EqualTo(TokenType.EndOfFile));
        }

        [Test]
        public void Position_is_initially_correct()
        {
            var source = StringToMemory("one two\r\n  three");
            var lexer = new Lexer(source);
            
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(0, 1, 0)));
        }

        [Test]
        public void Position_is_updated_when_tokens_are_read()
        {
            var source = StringToMemory("one two\r\n\n  three");
            var lexer = new Lexer(source);

            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(0, 1, 0)));
            lexer.GetToken();
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(4, 1, 4)));
            lexer.GetToken();
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(12, 3, 2)));
            lexer.GetToken();
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(17, 3, 7)));
        }

        [Test]
        public void Position_is_updated_in_string_literal()
        {
            var source = StringToMemory("\"something\" something");
            var lexer = new Lexer(source);

            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(0, 1, 0)));
            lexer.GetToken();
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(12, 1, 12)));
        }

        [Test]
        public void Byte_order_mark_is_ignored()
        {
            var source = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'o', (byte)'n', (byte)'e' };
            var lexer = new Lexer(source.AsMemory());

            Assert.That(Utf8ToString(lexer.PeekToken()), Is.EqualTo("one"));
            Assert.That(lexer.Position, Is.EqualTo(new TextPosition(3, 1, 0)));
        }

        [Test]
        public void Semicolon_is_considered_token()
        {
            var source = StringToMemory("something;");
            var lexer = new Lexer(source);

            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo("something"));
            Assert.That(Utf8ToString(lexer.GetToken()), Is.EqualTo(";"));
        }

        /// <summary>
        /// Converts a UTF-16 encoded source code string into a view of a UTF-8 array.
        /// </summary>
        private static Memory<byte> StringToMemory([NotNull] string source)
        {
            return Encoding.UTF8.GetBytes(source).AsMemory();
        }

        /// <summary>
        /// Converts a UTF-8 encoded span into a UTF-16 string.
        /// </summary>
        [NotNull]
        private static string Utf8ToString(ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
    }
}
