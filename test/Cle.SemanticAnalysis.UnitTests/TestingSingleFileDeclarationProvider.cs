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
        
        public bool GetMethod(string name, out MethodDeclaration method)
        {
            return Methods.TryGetValue(name, out method);
        }
    }
}
