using System.Collections.Generic;

namespace Cle.SemanticAnalysis.UnitTests
{
    /// <summary>
    /// A simple <see cref="IDeclarationProvider"/> implementation that does not know about modules or namespaces.
    /// </summary>
    internal class TestingSingleFileDeclarationProvider : IDeclarationProvider
    {
        /// <summary>
        /// Gets a mutable dictionary of methods indexed by name.
        /// </summary>
        public Dictionary<string, MethodDeclaration> Methods { get; } = new Dictionary<string, MethodDeclaration>();

        public IReadOnlyList<MethodDeclaration> GetMethodDeclarations(
            string methodName, 
            IReadOnlyList<string> visibleNamespaces,
            string sourceFile)
        {
            // Namespaces, bah
            return Methods.TryGetValue(methodName, out var result) ? new [] { result } : new MethodDeclaration[] { };
        }
    }
}
