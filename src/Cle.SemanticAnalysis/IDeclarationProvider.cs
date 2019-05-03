using System.Collections.Generic;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// This interface provides methods for getting and setting type and method information.
    /// </summary>
    public interface IDeclarationProvider
    {
        /// <summary>
        /// Returns a list of matching method declarations.
        /// </summary>
        /// <param name="methodName">The name of the method without namespace prefix.</param>
        /// <param name="visibleNamespaces">Namespaces available for searching the method.</param>
        /// <param name="sourceFile">The current source file, used for matching private methods.</param>
        // TODO: Specifying visible modules
        IReadOnlyList<MethodDeclaration> GetMethodDeclarations(
            string methodName,
            IReadOnlyList<string> visibleNamespaces,
            string sourceFile);
    }
}
