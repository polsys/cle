using System;
using Cle.Common;

namespace Cle.Frontend
{
    /// <summary>
    /// Pretty diagnostic messages.
    /// </summary>
    internal static class DiagnosticMessages
    {
        public static string GetMessage(Diagnostic diagnostic)
        {
            switch (diagnostic.Code)
            {
                // Parsing errors
                case DiagnosticCode.ParseErrorStart:
                    return "Unspecified syntax error.";
                case DiagnosticCode.ModuleNotFound:
                    return $"Module '{diagnostic.Module}' could not be found.";
                case DiagnosticCode.SourceFileNotFound:
                    return $"Source file '{diagnostic.Filename}' could not be found.";
                case DiagnosticCode.ExpectedSourceFileItem:
                    return $"Expected namespace, method or type definition, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedNamespaceDeclarationBeforeDefinitions:
                    return "Namespace must be declared before defining methods or types.";
                case DiagnosticCode.ExpectedOnlyOneNamespace:
                    return "Namespace may be declared only once per source file.";
                case DiagnosticCode.ExpectedNamespaceName:
                    return $"Expected namespace name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.InvalidNamespaceName:
                    return $"'{diagnostic.Actual}' is not a valid namespace name.";
                case DiagnosticCode.ExpectedVisibilityModifier:
                    return $"Expected definition preceded by a visibility modifier, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedType:
                    return $"Expected type name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedFunctionName:
                    return $"Expected method name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedParameterList:
                    return $"Expected parameter list, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedMethodBody:
                    return $"Expected method body, read '{diagnostic.Actual}'.";
                case DiagnosticCode.InvalidTypeName:
                    return $"'{diagnostic.Actual}' is not a valid type name.";
                case DiagnosticCode.InvalidFunctionName:
                    return $"'{diagnostic.Actual}' is not a valid method name.";
                case DiagnosticCode.ExpectedSemicolon:
                    return $"Expected ';', read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedClosingParen:
                    return $"Expected ')', read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedClosingBrace:
                    return $"Expected '}}', read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedStatement:
                    return $"Expected statement, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedExpression:
                    return $"Expected expression, read '{diagnostic.Actual}'.";
                case DiagnosticCode.InvalidNumericLiteral:
                    return $"'{diagnostic.Actual}' is not a valid numeric literal.";
                case DiagnosticCode.ExpectedCondition:
                    return $"Expected condition surrounded by parentheses, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedBlock:
                    return $"Expected block, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedBlockOrElseIf:
                    return $"Expected block or 'else if', read '{diagnostic.Actual}'.";
                case DiagnosticCode.ElseWithoutIf:
                    return "Else statement without corresponding if statement.";

                // Parse warnings
                case DiagnosticCode.ParseWarningStart:
                    return "Unspecified syntax warning.";

                // Semantic errors
                case DiagnosticCode.SemanticErrorStart:
                    return "Unspecified semantic error.";
                case DiagnosticCode.TypeNotFound:
                    return $"Type {diagnostic.Actual} does not exist or is not visible in this file.";
                case DiagnosticCode.MethodAlreadyDefined:
                    return $"Method {diagnostic.Actual} is already defined in this scope.";
                case DiagnosticCode.TypeMismatch:
                    return $"Expression is of type {diagnostic.Actual} but expected {diagnostic.Expected}.";
                case DiagnosticCode.IntegerLiteralOutOfBounds:
                    return $"Integer literal {diagnostic.Actual} cannot be represented in {diagnostic.Expected}.";

                // Semantic warnings
                case DiagnosticCode.SemanticWarningStart:
                    return "Unspecified semantic warning.";

                // Backend errors
                case DiagnosticCode.BackendErrorStart:
                    return "Unspecified backend error.";

                // Backend warnings
                case DiagnosticCode.BackendWarningStart:
                    return "Unspecified backend warning.";

                default:
                    throw new ArgumentOutOfRangeException(nameof(diagnostic), "Unimplemented diagnostic code");
            }
        }
    }
}
