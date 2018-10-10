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

                // Parse warnings
                case DiagnosticCode.ParseWarningStart:
                    return "Unspecified syntax warning.";

                // Semantic errors
                case DiagnosticCode.SemanticErrorStart:
                    return "Unspecified semantic error.";

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
