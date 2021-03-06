﻿using System;
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
                    return $"Source file '{diagnostic.Filename}' could not be opened.";
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
                case DiagnosticCode.ExpectedAttributeName:
                    return $"Expected attribute name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.AttributesOnlyApplicableToFunctions:
                    return "Attributes can only be applied to functions.";
                case DiagnosticCode.AttributeParameterMustBeLiteral:
                    return "Attribute parameters must be literals.";
                case DiagnosticCode.ExpectedVisibilityModifier:
                    return $"Expected definition preceded by a visibility modifier, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedType:
                    return $"Expected type name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedFunctionName:
                    return $"Expected method name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedParameterList:
                    return $"Expected parameter list, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedParameterDeclaration:
                    return $"Expected parameter declaration, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedParameterName:
                    return $"Expected parameter name, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedMethodBody:
                    return $"Expected method body or semicolon, read '{diagnostic.Actual}'.";
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
                case DiagnosticCode.ExpectedClosingBracket:
                    return $"Expected ']', read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedStatement:
                    return $"Expected statement, read '{diagnostic.Actual}'.";
                case DiagnosticCode.ExpectedExpression:
                    return $"Expected expression, read '{diagnostic.Actual}'.";
                case DiagnosticCode.InvalidNumericLiteral:
                    return $"'{diagnostic.Actual}' is not a valid numeric literal.";
                case DiagnosticCode.ExpectedClosingQuote:
                    return "Expected a closing quote for this string literal before end of line.";
                case DiagnosticCode.ExpectedInitialValue:
                    return "Expected initial value for variable.";
                case DiagnosticCode.ExpectedIdentifier:
                    return $"Expected identifier, read '{diagnostic.Actual}'.";
                case DiagnosticCode.InvalidVariableName:
                    return $"'{diagnostic.Actual}' is not a valid variable name.";
                case DiagnosticCode.InvalidIdentifier:
                    return $"'{diagnostic.Actual}' is not a valid variable or method name.";
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
                case DiagnosticCode.VoidIsNotValidType:
                    return $"Void is only valid as a return type, but is here used for {diagnostic.Actual}.";
                case DiagnosticCode.MethodAlreadyDefined:
                    return $"Method {diagnostic.Actual} is already defined in this scope.";
                case DiagnosticCode.UnknownAttribute:
                    return $"Attribute {diagnostic.Actual} is not known by the compiler.";
                case DiagnosticCode.EntryPointMustBeDeclaredCorrectly:
                    return "A method marked as an entry point must return int32 and take no parameters.";
                case DiagnosticCode.NoEntryPointProvided:
                    return "The main module does not contain a method marked as the entry point.";
                case DiagnosticCode.MultipleEntryPointsProvided:
                    return "The main module already has an entry point.";
                case DiagnosticCode.EntryPointAndImportNotCompatible:
                    return "The [EntryPoint] and [Import] attributes may not be applied to the same method.";
                case DiagnosticCode.ImportParameterNotValid:
                    return "Expected a string literal that is not empty and only contains ASCII characters.";
                case DiagnosticCode.TypeMismatch:
                    return $"Expression is of type {diagnostic.Actual} but expected {diagnostic.Expected}.";
                case DiagnosticCode.IntegerConstantOutOfBounds:
                    return $"Integer expression typed as {diagnostic.Expected} overflows at compile time.";
                case DiagnosticCode.DivisionByConstantZero:
                    return "Division by constant zero.";
                case DiagnosticCode.OperatorNotDefined:
                    return $"The operator '{diagnostic.Actual}' is not defined for type {diagnostic.Expected}.";
                case DiagnosticCode.VariableAlreadyDefined:
                    return $"Variable '{diagnostic.Actual}' is already defined in this or enclosing scope.";
                case DiagnosticCode.VariableNotFound:
                    return $"Variable '{diagnostic.Actual}' does not exist in this or enclosing scope.";
                case DiagnosticCode.MethodNotFound:
                    return $"Method '{diagnostic.Actual}' is not visible in this scope or does not exist.";
                case DiagnosticCode.ParameterCountMismatch:
                    return $"Expected {diagnostic.Expected} parameters but there are {diagnostic.Actual}.";
                case DiagnosticCode.ReturnNotGuaranteed:
                    return $"Not all code paths in method '{diagnostic.Actual}' are guaranteed to return.";

                // Semantic warnings
                case DiagnosticCode.SemanticWarningStart:
                    return "Unspecified semantic warning.";
                case DiagnosticCode.UnreachableCode:
                    return "This statement is unreachable and can be removed.";

                // Backend errors
                case DiagnosticCode.BackendErrorStart:
                    return "Unspecified backend error.";
                case DiagnosticCode.CouldNotCreateOutputFile:
                    return $"Could create output file for the module {diagnostic.Module}.";

                // Backend warnings
                case DiagnosticCode.BackendWarningStart:
                    return "Unspecified backend warning.";

                default:
                    throw new ArgumentOutOfRangeException(nameof(diagnostic), "Unimplemented diagnostic code");
            }
        }
    }
}
