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

        ParseWarningStart = 1500,

        SemanticErrorStart = 2000,

        SemanticWarningStart = 2500,

        BackendErrorStart = 3000,

        BackendWarningStart = 3500,
    }
}
