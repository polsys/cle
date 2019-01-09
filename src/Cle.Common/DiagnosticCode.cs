namespace Cle.Common
{
    /// <summary>
    /// The numerical codes for compiler warnings and errors.
    /// </summary>
    public enum DiagnosticCode
    {
        ParseErrorStart = 1000,

        ModuleNotFound,
        SourceFileNotFound,

        ExpectedSourceFileItem,
        ExpectedNamespaceDeclarationBeforeDefinitions,
        ExpectedOnlyOneNamespace,
        ExpectedNamespaceName,
        InvalidNamespaceName,

        ExpectedAttributeName,
        AttributesOnlyApplicableToFunctions,
        ExpectedVisibilityModifier,
        ExpectedType,
        ExpectedFunctionName,
        ExpectedParameterList,
        ExpectedParameterDeclaration,
        ExpectedParameterName,
        ExpectedMethodBody,
        InvalidTypeName,
        InvalidFunctionName,

        ExpectedSemicolon,
        ExpectedClosingParen,
        ExpectedClosingBrace,
        ExpectedClosingBracket,

        ExpectedStatement,
        ExpectedExpression,
        InvalidNumericLiteral,
        ExpectedInitialValue,
        InvalidVariableName,
        ExpectedCondition,
        ExpectedBlock,
        ExpectedBlockOrElseIf,
        ElseWithoutIf,

        ParseWarningStart = 1500,

        SemanticErrorStart = 2000,

        TypeNotFound,
        MethodAlreadyDefined,
        UnknownAttribute,
        EntryPointMustBeDeclaredCorrectly,
        NoEntryPointProvided,
        MultipleEntryPointsProvided,

        TypeMismatch,
        IntegerConstantOutOfBounds,
        DivisionByConstantZero,
        OperatorNotDefined,
        VariableAlreadyDefined,
        VariableNotFound,

        ReturnNotGuaranteed,

        SemanticWarningStart = 2500,

        UnreachableCode,

        BackendErrorStart = 3000,

        BackendWarningStart = 3500,
    }
}
