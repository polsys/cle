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
        AttributeParameterMustBeLiteral,
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
        ExpectedClosingQuote,
        ExpectedInitialValue,
        InvalidVariableName,
        ExpectedIdentifier,
        InvalidIdentifier,
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
        EntryPointAndImportNotCompatible,
        ImportParameterNotValid,

        TypeMismatch,
        VoidIsNotValidType,
        IntegerConstantOutOfBounds,
        DivisionByConstantZero,
        OperatorNotDefined,
        VariableAlreadyDefined,
        VariableNotFound,
        MethodNotFound,
        ParameterCountMismatch,

        ReturnNotGuaranteed,

        SemanticWarningStart = 2500,

        UnreachableCode,

        BackendErrorStart = 3000,
        CouldNotCreateOutputFile,

        BackendWarningStart = 3500,
    }
}
