using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
        [NotNull] private readonly string _filename;
        [NotNull] private readonly IDiagnosticSink _diagnosticSink;

        /// <summary>
        /// Internal for testing only.
        /// Use <see cref="Parse"/> instead.
        /// </summary>
        internal SyntaxParser(Memory<byte> source, [NotNull] string filename, [NotNull] IDiagnosticSink diagnosticSink)
        {
            _lexer = new Lexer(source);
            _filename = filename;
            _diagnosticSink = diagnosticSink;
        }

        /// <summary>
        /// Tries to parse the given source file.
        /// If the file cannot be parsed correctly, returns null.
        /// </summary>
        /// <param name="source">The UTF-8 encoded source file content.</param>
        /// <param name="filename">The name of the source file.</param>
        /// <param name="diagnosticSink">The sink to write parse diagnostics into.</param>
        [CanBeNull]
        public static SourceFileSyntax Parse(
            Memory<byte> source, 
            [NotNull] string filename,
            [NotNull] IDiagnosticSink diagnosticSink)
        {
            var parser = new SyntaxParser(source, filename, diagnosticSink);
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

                    // Method body
                    if (_lexer.PeekTokenType() != TokenType.OpenBrace)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedMethodBody, _lexer.Position, ReadTokenIntoString());
                        return null;
                    }
                    if (!TryParseBlock(out var methodBody))
                    {
                        return null;
                    }

                    // Add the parsed function to the syntax tree
                    Debug.Assert(methodBody != null);
                    functionListBuilder.Add(new FunctionSyntax(functionName, typeName, visibility, methodBody, itemPosition));
                }
                else
                {
                    _diagnosticSink.Add(DiagnosticCode.ExpectedSourceFileItem, itemPosition);
                    return null;
                }
            }

            return new SourceFileSyntax(namespaceName, _filename, functionListBuilder.ToImmutable());
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
        
        private bool TryParseBlock([CanBeNull] out BlockSyntax block)
        {
            block = null;

            // Eat the opening brace
            var startPosition = _lexer.Position;
            Debug.Assert(_lexer.PeekTokenType() == TokenType.OpenBrace);
            _lexer.GetToken();

            // Parse statements until a closing brace is found
            var statementList = ImmutableList<StatementSyntax>.Empty.ToBuilder();

            while (_lexer.PeekTokenType() != TokenType.CloseBrace)
            {
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Else:
                        _diagnosticSink.Add(DiagnosticCode.ElseWithoutIf, _lexer.Position);
                        return false;
                    case TokenType.If:
                        if (TryParseIf(out var ifSyntax))
                        {
                            statementList.Add(ifSyntax);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    case TokenType.OpenBrace:
                        if (TryParseBlock(out var innerBlockSyntax))
                        {
                            statementList.Add(innerBlockSyntax);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    case TokenType.Return:
                        if (TryParseReturnStatement(out var returnStatement))
                        {
                            statementList.Add(returnStatement);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    case TokenType.Identifier:
                        if (TryParseStatementStartingWithIdentifier(out var statement))
                        {
                            statementList.Add(statement);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    case TokenType.EndOfFile:
                        _diagnosticSink.Add(DiagnosticCode.ExpectedClosingBrace, _lexer.Position, ReadTokenIntoString());
                        return false;
                    default:
                        // TODO: Think about error recovery
                        _diagnosticSink.Add(DiagnosticCode.ExpectedStatement, _lexer.Position, ReadTokenIntoString());
                        return false;
                }
            }

            // Eat the closing brace
            Debug.Assert(_lexer.PeekTokenType() == TokenType.CloseBrace);
            _lexer.GetToken();

            block = new BlockSyntax(statementList.ToImmutable(), startPosition);
            return true;
        }

        private bool TryParseIf([CanBeNull] out IfStatementSyntax ifStatement)
        {
            ifStatement = null;

            // Eat the 'if' keyword
            var startPosition = _lexer.Position;
            Debug.Assert(_lexer.PeekTokenType() == TokenType.If);
            _lexer.GetToken();

            // Read the condition
            // TODO: Refactor this out when implementing other conditionals (like while)
            if (!ExpectToken(TokenType.OpenParen, DiagnosticCode.ExpectedCondition) ||
                !TryParseExpression(out var condition) ||
                !ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
            {
                return false;
            }
            Debug.Assert(condition != null);

            // Read the block - single statements are not allowed
            if (_lexer.PeekTokenType() != TokenType.OpenBrace)
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedBlock, _lexer.Position, ReadTokenIntoString());
                return false;
            }
            if (!TryParseBlock(out var thenBlock))
            {
                return false;
            }
            Debug.Assert(thenBlock != null);

            // If there is no else statement, early out
            // Else, read the 'else' keyword
            if (_lexer.PeekTokenType() != TokenType.Else)
            {
                ifStatement = new IfStatementSyntax(condition, thenBlock, null, startPosition);
                return true;
            }
            _lexer.GetToken();

            // There are two cases:
            //   - plain unconditional else with a block (again, no single statements)
            //   - 'else if' where we recurse into another 'if'
            if (_lexer.PeekTokenType() == TokenType.OpenBrace)
            {
                if (!TryParseBlock(out var elseBlock))
                {
                    return false;
                }
                Debug.Assert(elseBlock != null);

                ifStatement = new IfStatementSyntax(condition, thenBlock, elseBlock, startPosition);
                return true;
            }
            else if (_lexer.PeekTokenType() == TokenType.If)
            {
                if (!TryParseIf(out var elseIf))
                {
                    return false;
                }
                Debug.Assert(elseIf != null);
                
                ifStatement = new IfStatementSyntax(condition, thenBlock, elseIf, startPosition);
                return true;
            }
            else
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedBlockOrElseIf, _lexer.Position, ReadTokenIntoString());
                return false;
            }
        }

        private bool TryParseReturnStatement([CanBeNull] out ReturnStatementSyntax returnStatement)
        {
            returnStatement = null;

            // Eat the 'return' keyword
            var startPosition = _lexer.Position;
            Debug.Assert(_lexer.PeekTokenType() == TokenType.Return);
            _lexer.GetToken();

            // There are two kinds of returns: void return and value return.
            // In case of the former, the keyword is immediately followed by a semicolon.
            // In case of the latter, parse the expression.
            ExpressionSyntax expression = null;
            if (_lexer.PeekTokenType() != TokenType.Semicolon)
            {
                if (!TryParseExpression(out expression))
                {
                    return false;
                }
            }

            // Eat the semicolon
            if (!ExpectToken(TokenType.Semicolon, DiagnosticCode.ExpectedSemicolon))
            {
                return false;
            }

            returnStatement = new ReturnStatementSyntax(expression, startPosition);
            return true;
        }

        private bool TryParseStatementStartingWithIdentifier(out StatementSyntax statement)
        {
            // This method handles
            //   - variable declarations ("int32 name = expression;")
            //   - assignments ("name = expression;")
            //   - standalone method calls ("name(...);") (TODO)
            statement = null;

            // Read the first identifier
            // TODO: Assignments may target struct fields too
            var startPosition = _lexer.Position;
            Debug.Assert(_lexer.PeekTokenType() == TokenType.Identifier);
            var firstIdentifier = ReadTokenIntoString();

            // Depending on the next token, decide what to do
            switch (_lexer.PeekTokenType())
            {
                case TokenType.Identifier:
                    // Variable declaration
                    var variableName = ReadTokenIntoString();

                    // Validate the type name
                    if (!NameParsing.IsValidFullName(firstIdentifier))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidTypeName, startPosition, firstIdentifier);
                        return false;
                    }

                    // Validate the variable name (the latter check disallows 'int32 int32 = 0;')
                    if (!NameParsing.IsValidSimpleName(variableName) || NameParsing.IsReservedTypeName(variableName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidVariableName, _lexer.LastPosition, variableName);
                        return false;
                    }

                    // Read the initial value (both = and the expression)
                    if (!ExpectToken(TokenType.Equals, DiagnosticCode.ExpectedInitialValue) ||
                        !TryParseExpression(out var initialValue))
                    {
                        return false;
                    }
                    Debug.Assert(initialValue != null);

                    // The statement is valid, just check for the semicolon before returning
                    statement = new VariableDeclarationSyntax(firstIdentifier, variableName, initialValue, startPosition);
                    break;

                case TokenType.Equals:
                    // Eat the '=' and read the new value
                    _lexer.GetToken();
                    if (!TryParseExpression(out var newValue))
                    {
                        return false;
                    }
                    Debug.Assert(newValue != null);

                    statement = new AssignmentSyntax(firstIdentifier, newValue, startPosition);
                    break;

                default:
                    // Note: the diagnostic is intentionally emitted at expected statement start position
                    _diagnosticSink.Add(DiagnosticCode.ExpectedStatement, startPosition, ReadTokenIntoString());
                    return false;
            }

            // This check is shared by all valid cases of the above switch
            if (!ExpectToken(TokenType.Semicolon, DiagnosticCode.ExpectedSemicolon))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Internal for testing only.
        /// This function handles error logging on its own.
        /// </summary>
        internal bool TryParseExpression([CanBeNull] out ExpressionSyntax expressionSyntax)
        {
            // TODO: Relative operators
            return TryParseArithmeticExpression(out expressionSyntax);
        }
        
        private bool TryParseArithmeticExpression([CanBeNull] out ExpressionSyntax expressionSyntax)
        {
            // Arithmetic expression := Arithmetic expression [+-] Term
            expressionSyntax = null;

            // Read the initial term
            if (!TryParseTerm(out var currentSyntax))
            {
                return false;
            }
            Debug.Assert(currentSyntax != null);

            // Recurse left
            while (true)
            {
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Plus:
                    {
                        if (!ParseOperation(BinaryOperation.Plus))
                            return false;
                        break;
                    }
                    case TokenType.Minus:
                    {
                        if (!ParseOperation(BinaryOperation.Minus))
                            return false;
                        break;
                    }
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }
            }

            // Local helper method for sharing code between the '+' and '-' paths
            bool ParseOperation(BinaryOperation operation)
            {
                // Eat the '+'/'-' token
                var operatorPosition = _lexer.Position;
                _lexer.GetToken();

                // Read the right operand
                if (!TryParseTerm(out var right))
                {
                    return false;
                }
                Debug.Assert(right != null);

                // Update the result
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseTerm([CanBeNull] out ExpressionSyntax expressionSyntax)
        {
            // Term := Term [+-] Factor
            expressionSyntax = null;

            // Read the initial term
            if (!TryParseFactor(out var currentSyntax))
            {
                return false;
            }
            Debug.Assert(currentSyntax != null);

            // Recurse left
            while (true)
            {
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Asterisk:
                    {
                        if (!ParseOperation(BinaryOperation.Times))
                            return false;
                        break;
                    }
                    case TokenType.ForwardSlash:
                    {
                        if (!ParseOperation(BinaryOperation.Divide))
                            return false;
                        break;
                    }
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }
            }

            // Local helper method for sharing code between the '*' and '/' paths
            bool ParseOperation(BinaryOperation operation)
            {
                // Eat the '*'/'/' token
                var operatorPosition = _lexer.Position;
                _lexer.GetToken();

                // Read the right operand
                if (!TryParseFactor(out var right))
                {
                    return false;
                }
                Debug.Assert(right != null);

                // Update the result
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseFactor([CanBeNull] out ExpressionSyntax expressionSyntax)
        {
            // Factor := Number | Boolean literal | ( Expression ) | -Factor
            
            expressionSyntax = null;

            switch (_lexer.PeekTokenType())
            {
                case TokenType.Minus:
                    // Eat the minus
                    var minusPosition = _lexer.Position;
                    _lexer.GetToken();

                    // Recurse to the inner factor
                    if (TryParseFactor(out var innerFactor))
                    {
                        Debug.Assert(innerFactor != null);
                        expressionSyntax = new UnaryExpressionSyntax(UnaryOperation.Minus, innerFactor, minusPosition);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case TokenType.OpenParen:
                    // Eat the '('
                    _lexer.GetToken();

                    // Parse the expression.
                    // The inner expression is returned as is, with no enclosing 'parens' node.
                    // The modified precedence/associativity is already reflected in the syntax tree.
                    if (!TryParseExpression(out expressionSyntax))
                    {
                        return false;
                    }

                    // Eat the ')'
                    if (!ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
                    {
                        return false;
                    }
                    return true;
                case TokenType.False:
                    // Eat the token
                    _lexer.GetToken();

                    expressionSyntax = new BooleanLiteralSyntax(false, _lexer.LastPosition);
                    return true;
                case TokenType.True:
                    // Eat the token
                    _lexer.GetToken();

                    expressionSyntax = new BooleanLiteralSyntax(true, _lexer.LastPosition);
                    return true;
                case TokenType.Identifier:
                    var variableName = ReadTokenIntoString();

                    if (!NameParsing.IsValidFullName(variableName) || NameParsing.IsReservedTypeName(variableName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidVariableName, _lexer.LastPosition, variableName);
                        return false;
                    }
                    else
                    {
                        expressionSyntax = new NamedValueSyntax(variableName, _lexer.LastPosition);
                        return true;
                    }
                default:
                    return TryParseNumber(out expressionSyntax);
            }
        }

        private bool TryParseNumber([CanBeNull] out ExpressionSyntax literalSyntax)
        {
            literalSyntax = null;

            if (_lexer.PeekTokenType() == TokenType.Number)
            {
                var numberToken = ReadTokenIntoString();
                if (ulong.TryParse(numberToken, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var number))
                {
                    literalSyntax = new IntegerLiteralSyntax(number, _lexer.LastPosition);
                    return true;
                }
                else
                {
                    _diagnosticSink.Add(DiagnosticCode.InvalidNumericLiteral, _lexer.LastPosition, numberToken);
                    return false;
                }
            }
            else
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedExpression, _lexer.Position, ReadTokenIntoString());
                return false;
            }
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
