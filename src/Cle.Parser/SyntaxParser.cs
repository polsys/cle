using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Cle.Common;
using Cle.Parser.SyntaxTree;

namespace Cle.Parser
{
    /// <summary>
    /// This class parses a single source file into a parse tree.
    /// </summary>
    public class SyntaxParser
    {
        private readonly Lexer _lexer;
        private readonly string _filename;
        private readonly IDiagnosticSink _diagnosticSink;

        /// <summary>
        /// Internal for testing only.
        /// Use <see cref="Parse"/> instead.
        /// </summary>
        internal SyntaxParser(Memory<byte> source, string filename, IDiagnosticSink diagnosticSink)
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
        public static SourceFileSyntax? Parse(
            Memory<byte> source, 
            string filename,
            IDiagnosticSink diagnosticSink)
        {
            var parser = new SyntaxParser(source, filename, diagnosticSink);
            return parser.ParseSourceFile();
        }

        private SourceFileSyntax? ParseSourceFile()
        {
            var namespaceName = string.Empty;
            var functionListBuilder = ImmutableList<FunctionSyntax>.Empty.ToBuilder();

            // Parse source file level items until end-of-file
            while (!_lexer.PeekToken().IsEmpty)
            {
                // First, parse any attributes
                var attributes = ImmutableList<AttributeSyntax>.Empty;
                while (_lexer.PeekTokenType() == TokenType.OpenBracket)
                {
                    if (TryParseAttribute(out var attribute))
                    {
                        Debug.Assert(attribute != null);
                        attributes = attributes.Add(attribute);
                    }
                    else
                    {
                        return null;
                    }
                }

                // Then read the item
                var itemPosition = _lexer.Position;

                if (_lexer.PeekTokenType() == TokenType.Namespace)
                {
                    // Attributes may not be applied to namespaces
                    if (!attributes.IsEmpty)
                    {
                        _diagnosticSink.Add(DiagnosticCode.AttributesOnlyApplicableToFunctions, itemPosition);
                        return null;
                    }

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
                    if (!TryParseType(DiagnosticCode.ExpectedType, out var returnType))
                    {
                        return null; // TODO: Recovery
                    }

                    // Name
                    if (!ExpectIdentifier(DiagnosticCode.ExpectedFunctionName, out var functionName))
                    {
                        return null;
                    }
                    if (!NameParsing.IsValidSimpleName(functionName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidFunctionName, _lexer.LastPosition, functionName);
                        return null; // TODO: Recovery
                    }

                    // Parameter list
                    if (!TryParseParameterDeclarations(out var parameters))
                    {
                        return null;
                    }

                    // Method body; the method may also have no body, as indicated by a semicolon
                    BlockSyntax? methodBody;
                    if (_lexer.PeekTokenType() == TokenType.Semicolon)
                    {
                        _lexer.GetToken(); // Eat the semicolon
                        methodBody = null;
                    }
                    else if (_lexer.PeekTokenType() != TokenType.OpenBrace)
                    {
                        _diagnosticSink.Add(DiagnosticCode.ExpectedMethodBody, _lexer.Position, ReadTokenIntoString());
                        return null;
                    }
                    else if (!TryParseBlock(out methodBody))
                    {
                        return null;
                    }

                    // Add the parsed function to the syntax tree
                    functionListBuilder.Add(new FunctionSyntax(functionName, returnType, visibility, 
                        parameters, attributes, methodBody, itemPosition));
                }
                else
                {
                    _diagnosticSink.Add(DiagnosticCode.ExpectedSourceFileItem, itemPosition);
                    return null;
                }
            }

            return new SourceFileSyntax(namespaceName, _filename, functionListBuilder.ToImmutable());
        }

        private bool TryParseAttribute([NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = null;

            // Eat the '['
            var startPosition = EatAndAssertToken(TokenType.OpenBracket);

            // Read the attribute name
            if (!ExpectIdentifier(DiagnosticCode.ExpectedAttributeName, out var attributeName))
            {
                return false;
            }

            // Read the parameter list, if one is given
            var parameterExpressions = ImmutableList<ExpressionSyntax>.Empty;
            if (_lexer.PeekTokenType() == TokenType.OpenParen && !TryParseParameterList(out parameterExpressions))
            {
                return false;
            }

            // Assert that each parameter is a literal.
            // This is done in order to simplify attribute resolution: no need to evaluate constants
            // before evaluating attributes.
            // TODO PERF: When replacing the immutable list data structure, add size hinting here
            var parameterLiterals = ImmutableList<LiteralSyntax>.Empty;
            foreach (var paramExpr in parameterExpressions)
            {
                if (paramExpr is LiteralSyntax literal)
                {
                    parameterLiterals = parameterLiterals.Add(literal);
                }
                else
                {
                    _diagnosticSink.Add(DiagnosticCode.AttributeParameterMustBeLiteral, paramExpr.Position);
                    return false;
                }
            }

            // Eat the closing bracket
            if (!ExpectToken(TokenType.CloseBracket, DiagnosticCode.ExpectedClosingBracket))
            {
                return false;
            }

            attribute = new AttributeSyntax(attributeName, parameterLiterals, startPosition);
            return true;
        }

        private bool TryParseNamespaceDeclaration(out string namespaceName)
        {
            // Eat the 'namespace' token
            EatAndAssertToken(TokenType.Namespace);

            // Read and validate the namespace name
            if (!ExpectIdentifier(DiagnosticCode.ExpectedNamespaceName, out namespaceName))
            {
                return false;
            }
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

        private bool TryParseParameterDeclarations(out ImmutableList<ParameterDeclarationSyntax> parameters)
        {
            parameters = ImmutableList<ParameterDeclarationSyntax>.Empty;

            if (!ExpectToken(TokenType.OpenParen, DiagnosticCode.ExpectedParameterList))
            {
                return false;
            }

            // Read parameters unless the parameter list is empty
            if (_lexer.PeekTokenType() != TokenType.CloseParen)
            {
                while (true)
                {
                    var paramPosition = _lexer.Position;

                    // Read the type name
                    if (!TryParseType(DiagnosticCode.ExpectedParameterDeclaration, out var paramType))
                    {
                        return false;
                    }

                    // Read the parameter name
                    if (!ExpectIdentifier(DiagnosticCode.ExpectedParameterName, out var paramName))
                    {
                        return false;
                    }
                    if (!NameParsing.IsValidSimpleName(paramName) || NameParsing.IsReservedTypeName(paramName))
                    {
                        _diagnosticSink.Add(DiagnosticCode.InvalidVariableName, _lexer.LastPosition, paramName);
                        return false;
                    }

                    // Add it to the list
                    parameters = parameters.Add(new ParameterDeclarationSyntax(paramType, paramName, paramPosition));

                    // If the next token is a comma, there is another parameter to parse
                    if (_lexer.PeekTokenType() == TokenType.Comma)
                    {
                        _lexer.GetToken();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
            {
                return false;
            }

            return true;
        }
        
        private bool TryParseType(DiagnosticCode noIdentifierError, [NotNullWhen(true)] out TypeSyntax? type)
        {
            type = null;

            if (!ExpectIdentifier(noIdentifierError, out var typeName))
            {
                return false;
            }
            if (!NameParsing.IsValidFullName(typeName))
            {
                _diagnosticSink.Add(DiagnosticCode.InvalidTypeName, _lexer.LastPosition, typeName);
                return false;
            }

            type = new TypeNameSyntax(typeName, _lexer.LastPosition);
            return true;
        }

        private bool TryParseBlock([NotNullWhen(true)] out BlockSyntax? block)
        {
            block = null;

            // Eat the opening brace
            var startPosition = EatAndAssertToken(TokenType.OpenBrace);

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
                    case TokenType.Var:
                        if (TryParseVariableDeclaration(out var declaration))
                        {
                            statementList.Add(declaration);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    case TokenType.While:
                        if (TryParseWhileStatement(out var whileStatement))
                        {
                            statementList.Add(whileStatement);
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

        private bool TryParseIf([NotNullWhen(true)] out IfStatementSyntax? ifStatement)
        {
            ifStatement = null;

            // Eat the 'if' keyword
            var startPosition = EatAndAssertToken(TokenType.If);

            // Read the condition
            if (!TryParseCondition(out var condition))
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

        private bool TryParseReturnStatement([NotNullWhen(true)] out ReturnStatementSyntax? returnStatement)
        {
            returnStatement = null;

            // Eat the 'return' keyword
            var startPosition = EatAndAssertToken(TokenType.Return);

            // There are two kinds of returns: void return and value return.
            // In case of the former, the keyword is immediately followed by a semicolon.
            // In case of the latter, parse the expression.
            ExpressionSyntax? expression = null;
            if (_lexer.PeekTokenType() != TokenType.Semicolon && !TryParseExpression(out expression))
            {
                return false;
            }

            // Eat the semicolon
            if (!ExpectToken(TokenType.Semicolon, DiagnosticCode.ExpectedSemicolon))
            {
                return false;
            }

            returnStatement = new ReturnStatementSyntax(expression, startPosition);
            return true;
        }

        private bool TryParseWhileStatement([NotNullWhen(true)] out WhileStatementSyntax? whileStatement)
        {
            whileStatement = null;

            // Eat the 'while' keyword
            var startPosition = EatAndAssertToken(TokenType.While);

            // Parse the condition
            if (!TryParseCondition(out var condition))
            {
                return false;
            }
            Debug.Assert(condition != null);

            // Parse the body
            if (_lexer.PeekTokenType() != TokenType.OpenBrace)
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedBlock, _lexer.Position, ReadTokenIntoString());
                return false;
            }
            if (!TryParseBlock(out var body))
            {
                return false;
            }
            Debug.Assert(body != null);

            whileStatement = new WhileStatementSyntax(condition, body, startPosition);
            return true;
        }

        private bool TryParseCondition([NotNullWhen(true)] out ExpressionSyntax? condition)
        {
            if (!ExpectToken(TokenType.OpenParen, DiagnosticCode.ExpectedCondition) ||
                !TryParseExpression(out condition) ||
                !ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
            {
                condition = null;
                return false;
            }

            return true;
        }

        private bool TryParseVariableDeclaration([NotNullWhen(true)] out VariableDeclarationSyntax? declaration)
        {
            declaration = null;
            var startPosition = _lexer.Position;
            EatAndAssertToken(TokenType.Var);

            // Following the "var" keyword there is the type name
            if (!TryParseType(DiagnosticCode.ExpectedType, out var typeSyntax))
            {
                return false;
            }

            // Then there is the variable name, which is a simple name
            if (_lexer.PeekTokenType() != TokenType.Identifier)
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedIdentifier, _lexer.Position, ReadTokenIntoString());
                return false;
            }

            var variableName = ReadTokenIntoString();
            if (!NameParsing.IsValidSimpleName(variableName) || NameParsing.IsReservedTypeName(variableName))
            {
                _diagnosticSink.Add(DiagnosticCode.InvalidVariableName, _lexer.LastPosition, variableName);
                return false;
            }

            // Then there is the initial value (a "=" followed by an expression)
            if (!ExpectToken(TokenType.Equals, DiagnosticCode.ExpectedInitialValue) ||
                !TryParseExpression(out var initialValue))
            {
                return false;
            }
            Debug.Assert(initialValue != null);

            // Finally, a semicolon
            if (!ExpectToken(TokenType.Semicolon, DiagnosticCode.ExpectedSemicolon))
            {
                return false;
            }

            declaration = new VariableDeclarationSyntax(typeSyntax, variableName, initialValue, startPosition);
            return true;
        }

        private bool TryParseStatementStartingWithIdentifier([NotNullWhen(true)] out StatementSyntax? statement)
        {
            // This method handles
            //   - assignments ("name = expression;")
            //   - standalone method calls ("name(...);")
            statement = null;

            // Read the identifier
            // TODO: Assignments may target array elements or struct fields
            var startPosition = _lexer.Position;
            Debug.Assert(_lexer.PeekTokenType() == TokenType.Identifier);
            if (!TryReadAndValidateIdentifier(out var identifier, allowReservedTypeNames: false))
            {
                return false;
            }

            // Depending on the next token, decide what to do
            switch (_lexer.PeekTokenType())
            {
                case TokenType.Equals:
                    // Eat the '=' and read the new value
                    _lexer.GetToken();
                    if (!TryParseExpression(out var newValue))
                    {
                        return false;
                    }
                    Debug.Assert(newValue != null);

                    statement = new AssignmentSyntax(identifier, newValue, startPosition);
                    break;

                case TokenType.OpenParen:
                    // A standalone function call
                    if (!TryParseFunctionCall(identifier, out var callExpression))
                    {
                        return false;
                    }
                    else
                    {
                        Debug.Assert(callExpression != null);
                        statement = new FunctionCallStatementSyntax(callExpression, startPosition);
                        break;
                    }

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
        internal bool TryParseExpression([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            return TryParseLogicalExpression(out expressionSyntax);
        }

        private bool TryParseLogicalExpression([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Logical expression := Logical expression [& && | || ^] Relational expression
            expressionSyntax = null;

            // Read the initial expression
            if (!TryParseRelationalExpression(out var currentSyntax))
            {
                return false;
            }
            Debug.Assert(currentSyntax != null);

            // Recurse left
            while (true)
            {
                BinaryOperation op;
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Ampersand:
                        op = BinaryOperation.And;
                        break;
                    case TokenType.DoubleAmpersand:
                        op = BinaryOperation.ShortCircuitAnd;
                        break;
                    case TokenType.Bar:
                        op = BinaryOperation.Or;
                        break;
                    case TokenType.DoubleBar:
                        op = BinaryOperation.ShortCircuitOr;
                        break;
                    case TokenType.Circumflex:
                        op = BinaryOperation.Xor;
                        break;
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }

                if (!ParseOperation(op))
                    return false;
            }
            
            bool ParseOperation(BinaryOperation operation)
            {
                // Eat the operator
                var operatorPosition = _lexer.Position;
                _lexer.GetToken();

                // Read the right operand
                if (!TryParseRelationalExpression(out var right))
                {
                    return false;
                }
                Debug.Assert(right != null);

                // Update the result
                Debug.Assert(currentSyntax != null);
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseRelationalExpression([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Relational expression := Relational expression [== != < <= >= >] Shift expression
            expressionSyntax = null;

            // Read the initial expression
            if (!TryParseShiftExpression(out var currentSyntax))
            {
                return false;
            }
            Debug.Assert(currentSyntax != null);

            // Recurse left
            while (true)
            {
                BinaryOperation op;
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.DoubleEquals:
                        op = BinaryOperation.Equal;
                        break;
                    case TokenType.NotEquals:
                        op = BinaryOperation.NotEqual;
                        break;
                    case TokenType.LessThan:
                        op = BinaryOperation.LessThan;
                        break;
                    case TokenType.LessThanOrEquals:
                        op = BinaryOperation.LessThanOrEqual;
                        break;
                    case TokenType.GreaterThanOrEquals:
                        op = BinaryOperation.GreaterThanOrEqual;
                        break;
                    case TokenType.GreaterThan:
                        op = BinaryOperation.GreaterThan;
                        break;
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }

                if (!ParseOperation(op))
                    return false;
            }

            // Local helper method for sharing code between the operators
            bool ParseOperation(BinaryOperation operation)
            {
                // Eat the operator
                var operatorPosition = _lexer.Position;
                _lexer.GetToken();

                // Read the right operand
                if (!TryParseShiftExpression(out var right))
                {
                    return false;
                }
                Debug.Assert(right != null);

                // Update the result
                Debug.Assert(currentSyntax != null);
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseShiftExpression([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Shift expression := Shift expression [<< >>] Arithmetic expression
            expressionSyntax = null;

            // Read the initial expression
            if (!TryParseArithmeticExpression(out var currentSyntax))
            {
                return false;
            }
            Debug.Assert(currentSyntax != null);

            // Recurse left
            while (true)
            {
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.DoubleLessThan:
                    {
                        if (!ParseOperation(BinaryOperation.ShiftLeft))
                            return false;
                        break;
                    }
                    case TokenType.DoubleGreaterThan:
                    {
                        if (!ParseOperation(BinaryOperation.ShiftRight))
                            return false;
                        break;
                    }
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }
            }
            
            bool ParseOperation(BinaryOperation operation)
            {
                // Eat the operator
                var operatorPosition = _lexer.Position;
                _lexer.GetToken();

                // Read the right operand
                if (!TryParseArithmeticExpression(out var right))
                {
                    return false;
                }
                Debug.Assert(right != null);

                // Update the result
                Debug.Assert(currentSyntax != null);
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseArithmeticExpression([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Arithmetic expression := Arithmetic expression [+ -] Term
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
                BinaryOperation op;
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Plus:
                        op = BinaryOperation.Plus;
                        break;
                    case TokenType.Minus:
                        op = BinaryOperation.Minus;
                        break;
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }

                if (!ParseOperation(op))
                    return false;
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
                Debug.Assert(currentSyntax != null);
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseTerm([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Term := Term [* / %] Factor
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
                BinaryOperation op;
                switch (_lexer.PeekTokenType())
                {
                    case TokenType.Asterisk:
                        op = BinaryOperation.Times;
                        break;
                    case TokenType.ForwardSlash:
                        op = BinaryOperation.Divide;
                        break;
                    case TokenType.Percent:
                        op = BinaryOperation.Modulo;
                        break;
                    default:
                        expressionSyntax = currentSyntax;
                        return true;
                }

                if (!ParseOperation(op))
                    return false;
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
                Debug.Assert(currentSyntax != null);
                currentSyntax = new BinaryExpressionSyntax(operation, currentSyntax, right, operatorPosition);
                return true;
            }
        }

        private bool TryParseFactor([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            // Factor := Identifier | Number | Boolean literal | ( Expression ) | -Factor | !Factor | ~Factor
            
            expressionSyntax = null;

            switch (_lexer.PeekTokenType())
            {
                case TokenType.Minus:
                    return ParseUnary(UnaryOperation.Minus, out expressionSyntax);
                case TokenType.Exclamation:
                    return ParseUnary(UnaryOperation.Negation, out expressionSyntax);
                case TokenType.Tilde:
                    return ParseUnary(UnaryOperation.Complement, out expressionSyntax);
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
                case TokenType.StringLiteral:
                    return TryParseStringLiteral(out expressionSyntax);
                case TokenType.Identifier:
                    if (!TryReadAndValidateIdentifier(out var identifier, allowReservedTypeNames: false))
                    {
                        return false;
                    }

                    // This may be either a function call or a variable reference
                    if (_lexer.PeekTokenType() == TokenType.OpenParen)
                    {
                        // Function call
                        if (!TryParseFunctionCall(identifier, out var callSyntax))
                        {
                            return false;
                        }
                        else
                        {
                            Debug.Assert(callSyntax != null);
                            expressionSyntax = callSyntax;
                            return true;
                        }
                    }
                    else
                    {
                        // Variable reference
                        expressionSyntax = identifier;
                        return true;
                    }
                default:
                    return TryParseNumber(out expressionSyntax);
            }

            // Local helper method for sharing code between the various unary paths
            bool ParseUnary(UnaryOperation op, out ExpressionSyntax? syntax)
            {
                // Eat the operator
                var opPosition = _lexer.Position;
                _lexer.GetToken();

                // Recurse into the inner expression, which is a factor too
                if (TryParseFactor(out var innerFactor))
                {
                    Debug.Assert(innerFactor != null);
                    syntax = new UnaryExpressionSyntax(op, innerFactor, opPosition);
                    return true;
                }
                else
                {
                    syntax = null;
                    return false;
                }
            }
        }

        private bool TryParseNumber([NotNullWhen(true)] out ExpressionSyntax? literalSyntax)
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

        private bool TryParseStringLiteral([NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
        {
            Debug.Assert(_lexer.PeekTokenType() == TokenType.StringLiteral);
            expressionSyntax = null;

            var tokenPosition = _lexer.Position;
            var tokenSpan = _lexer.GetToken();

            // The string literal must end in a quote
            if (tokenSpan.Length < 2 || tokenSpan[tokenSpan.Length - 1] != (byte)'"')
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedClosingQuote, tokenPosition);
                return false;
            }

            // Strip the quotes
            // TODO: Process escape sequences
            expressionSyntax = new StringLiteralSyntax(tokenSpan.Slice(1, tokenSpan.Length - 2).ToArray(), tokenPosition);
            return true;
        }

        /// <summary>
        /// This function assumes that the function name, passed in <paramref name="functionName"/>,
        /// has already been read and validated, and the next token is known to be "(".
        /// </summary>
        private bool TryParseFunctionCall(IdentifierSyntax functionName,
            [NotNullWhen(true)] out FunctionCallSyntax? callSyntax)
        {
            callSyntax = null;
            var callPosition = _lexer.LastPosition;

            // Read the parameter list: this also eats the open paren
            if (!TryParseParameterList(out var parameters))
            {
                return false;
            }

            callSyntax = new FunctionCallSyntax(functionName, parameters, callPosition);
            return true;
        }

        /// <summary>
        /// Reads a parameter list for a function call or an attribute.
        /// Logs an error and returns false if the list cannot be parsed.
        /// Precondition: an open paren is the next token.
        /// </summary>
        private bool TryParseParameterList(out ImmutableList<ExpressionSyntax> parameters)
        {
            // Eat the open paren
            EatAndAssertToken(TokenType.OpenParen);

            // Read parameters unless the parameter list is empty
            parameters = ImmutableList<ExpressionSyntax>.Empty;
            if (_lexer.PeekTokenType() != TokenType.CloseParen)
            {
                while (true)
                {
                    // Read the expression
                    if (!TryParseExpression(out var param))
                    {
                        return false;
                    }
                    parameters = parameters.Add(param);

                    // If the next token is a comma, there is another parameter to parse
                    if (_lexer.PeekTokenType() == TokenType.Comma)
                    {
                        _lexer.GetToken();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Eat the closing paren
            if (!ExpectToken(TokenType.CloseParen, DiagnosticCode.ExpectedClosingParen))
            {
                return false;
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
        /// Eats a token and throws if the token type does not match.
        /// Returns the token position.
        /// </summary>
        private TextPosition EatAndAssertToken(TokenType expectedTokenType)
        {
            if (_lexer.PeekTokenType() != expectedTokenType)
            {
                throw new InvalidOperationException(
                    $"The method requires {expectedTokenType} but read {_lexer.PeekTokenType()}.");
            }
            _lexer.GetToken();

            return _lexer.LastPosition;
        }

        /// <summary>
        /// Eats a token and returns whether it is of the specified type.
        /// If it is not, additionally logs an error.
        /// </summary>
        /// <param name="expectedType">The expected token type.</param>
        /// <param name="errorCode">The error code to log if the token is not of the expected type.</param>
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
        /// Eats a token and returns whether it is an identifier.
        /// If it is not, additionally logs an error.
        /// </summary>
        /// <param name="errorCode">The error code to log if the token is not an identifier.</param>
        /// <param name="identifier">The read identifier.</param>
        private bool ExpectIdentifier(DiagnosticCode errorCode, out string identifier)
        {
            if (_lexer.PeekTokenType() == TokenType.Identifier)
            {
                identifier = ReadTokenIntoString();
                return true;
            }
            else
            {
                _diagnosticSink.Add(errorCode, _lexer.Position, ReadTokenIntoString());
                identifier = string.Empty;
                return false;
            }
        }

        private bool TryReadAndValidateIdentifier([NotNullWhen(true)] out IdentifierSyntax? identifier,
            bool allowReservedTypeNames)
        {
            if (_lexer.PeekTokenType() != TokenType.Identifier)
            {
                _diagnosticSink.Add(DiagnosticCode.ExpectedIdentifier, _lexer.Position, ReadTokenIntoString());
                identifier = null;
                return false;
            }

            var token = ReadTokenIntoString();
            if (!NameParsing.IsValidFullName(token) ||
                (!allowReservedTypeNames && NameParsing.IsReservedTypeName(token)))
            {
                _diagnosticSink.Add(DiagnosticCode.InvalidIdentifier, _lexer.LastPosition, token);
                identifier = null;
                return false;
            }

            identifier = new IdentifierSyntax(token, _lexer.LastPosition);
            return true;
        }

        /// <summary>
        /// Reads a token and converts it into an UTF-16 string.
        /// </summary>
        private string ReadTokenIntoString()
        {
            return Encoding.UTF8.GetString(_lexer.GetToken());
        }
    }
}
