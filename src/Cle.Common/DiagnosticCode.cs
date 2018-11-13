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

        ExpectedVisibilityModifier,
        ExpectedType,
        ExpectedFunctionName,
        ExpectedParameterList,
        ExpectedMethodBody,
        InvalidTypeName,
        InvalidFunctionName,

        ExpectedSemicolon,
        ExpectedClosingParen,
        ExpectedClosingBrace,

        ExpectedStatement,
        ExpectedExpression,
        InvalidNumericLiteral,
        ExpectedCondition,
        ExpectedBlock,
        ExpectedBlockOrElseIf,
        ElseWithoutIf,

        ParseWarningStart = 1500,

        SemanticErrorStart = 2000,

        TypeNotFound,

        SemanticWarningStart = 2500,

        BackendErrorStart = 3000,

        BackendWarningStart = 3500,
    }
}
