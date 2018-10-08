using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cle.Common;
using Cle.Parser.SyntaxTree;
using JetBrains.Annotations;

namespace Cle.Parser
{
    /// <summary>
    /// This class parses a single source file into a parse tree.
    /// </summary>
    public class SyntaxParser
    {
        [NotNull] private readonly Lexer _lexer;
        [NotNull] private readonly IDiagnosticSink _diagnosticSink;
        
        private SyntaxParser(Memory<byte> source, [NotNull] IDiagnosticSink diagnosticSink)
        {
            _lexer = new Lexer(source);
            _diagnosticSink = diagnosticSink;
        }

        /// <summary>
        /// Tries to parse the given source file.
        /// If the file cannot be parsed correctly, returns null.
        /// </summary>
        /// <param name="source">The UTF-8 encoded source file content.</param>
        /// <param name="diagnosticSink">The sink to write parse diagnostics into.</param>
        [CanBeNull]
        public static SourceFileSyntax Parse(Memory<byte> source, [NotNull] IDiagnosticSink diagnosticSink)
        {
            var parser = new SyntaxParser(source, diagnosticSink);
            return parser.ParseSourceFile();
        }

        [CanBeNull]
        private SourceFileSyntax ParseSourceFile()
        {
            var namespaceName = string.Empty;
            var functionListBuilder = ImmutableList<FunctionSyntax>.Empty.ToBuilder();

            // Parse source file level items until end-of-file
            while (!_lexer.PeekToken().IsEmpty)
            {
                var itemPosition = _lexer.Position;

                if (_lexer.PeekTokenType() == TokenType.Namespace)
                {
                    // Only a single namespace declaration is allowed per file
                    if (namespaceName != string.Empty)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedOnlyOneNamespace, itemPosition);
                        return null;
                    }

                    if (!TryParseNamespaceDeclaration(out namespaceName))
                    {
                        return null;
                    }

                    continue;
                }

                // Other source file items must begin with a visibility modifier
                var visibility = ParseVisibility();

                if (visibility != Visibility.Unknown)
                {
                    // Namespace must be declared before any definitions
                    if (namespaceName == string.Empty)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedNamespaceDeclarationBeforeDefinitions, itemPosition);
                        return null;
                    }

                    // TODO: Class definitions

                    // Parse the function definition: type
                    if (_lexer.PeekTokenType() != TokenType.Identifier)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedType, _lexer.Position, ReadTokenIntoString());
                        return null; // TODO: Try to recover from the error
                    }

                    var typeName = ReadTokenIntoString();
                    if (!NameParsing.IsValidFullName(typeName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidTypeName, _lexer.LastPosition, typeName);
                        return null; // TODO: Recovery
                    }

                    // Name
                    if (_lexer.PeekTokenType() != TokenType.Identifier)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedFunctionName, _lexer.Position, ReadTokenIntoString());
                        return null; // TODO: Try to recover from the error
                    }

                    var functionName = ReadTokenIntoString();
                    if (!NameParsing.IsValidSimpleName(functionName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidFunctionName, _lexer.LastPosition, functionName);
                        return null; // TODO: Recovery
                    }

                    // Parameter list
                    // TODO: Actually parse the parameter list
                    if (!ExpectToken(TokenType.OpenParen, DiagnosticCode.ExpectedParameterList))
                    {
                        return null; // TODO: Recovery
                    }
                    if (!ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
                    {
                        return null;
                    }

                    // Body
                    // TODO: Actually parse a block
                    if (!ExpectToken(TokenType.OpenBrace, DiagnosticCode.ExpectedMethodBody))
                    {
                        return null;
                    }
                    if (!ExpectToken(TokenType.CloseBrace, DiagnosticCode.ExpectedClosingBrace))
                    {
                        return null;
                    }

                    // Add the parsed function to the syntax tree
                    functionListBuilder.Add(new FunctionSyntax(functionName, typeName, visibility, itemPosition));
                }
                else
                {
                    _diagnosticSink.Add(DiagnosticCode.ExpectedSourceFileItem, itemPosition);
                    return null;
                }
            }

            return new SourceFileSyntax(namespaceName, functionListBuilder.ToImmutable());
        }

        private bool TryParseNamespaceDeclaration([NotNull] out string namespaceName)
        {
            namespaceName = string.Empty;

            // Eat the 'namespace' token
            Debug.Assert(_lexer.PeekTokenType() == TokenType.Namespace);
            _lexer.GetToken();

            // Read and validate the namespace name
            if (_lexer.PeekTokenType() != TokenType.Identifier)
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedNamespaceName, _lexer.Position, ReadTokenIntoString());
                return false;
            }

            namespaceName = ReadTokenIntoString();
            if (!NameParsing.IsValidNamespaceName(namespaceName))
            {
                _diagnosticSink.Add(DiagnosticCode.InvalidNamespaceName, _lexer.LastPosition, namespaceName);
                return false;
            }

            // Eat the semicolon
            if (!ExpectToken(TokenType.Semicolon, DiagnosticCode.ExpectedSemicolon))
            {
                return false; // TODO: Think about error recovery
            }

            return true;
        }

        /// <summary>
        /// Reads a visibility modifier.
        /// If the next token is not a visibility modifier, returns <see cref="Visibility.Unknown"/>
        /// and eats the token.
        /// </summary>
        private Visibility ParseVisibility()
        {
            switch (_lexer.GetTokenType())
            {
                case TokenType.Private:
                    return Visibility.Private;
                case TokenType.Internal:
                    return Visibility.Internal;
                case TokenType.Public:
                    return Visibility.Public;
                default:
                    return Visibility.Unknown;
            }
        }

        /// <summary>
        /// Eats a token and returns whether it is of the specified type.
        /// If it is not, additionally logs an error.
        /// </summary>
        /// <param name="expectedType">The expected token type.</param>
        /// <param name="errorCode">The error code to log if the token is not of the expected type.</param>
        /// <returns></returns>
        private bool ExpectToken(TokenType expectedType, DiagnosticCode errorCode)
        {
            if (_lexer.PeekTokenType() == expectedType)
            {
                _lexer.GetToken();
                return true;
            }
            else
            {
                _diagnosticSink.Add(errorCode, _lexer.Position, ReadTokenIntoString());
                return false;
            }
        }

        /// <summary>
        /// Reads a token and converts it into an UTF-16 string.
        /// </summary>
        [NotNull]
        private string ReadTokenIntoString()
        {
            // TODO: Remove ToArray() once the ReadOnlySpan<byte> overload is available
            return Encoding.UTF8.GetString(_lexer.GetToken().ToArray());
        }
    }
}
