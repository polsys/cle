using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// This interface provides methods for getting and setting type and method information.
    /// </summary>
    public interface IDeclarationProvider
    {
        /// <summary>
        /// Tries to get type information for the given method.
        /// </summary>
        /// <param name="name">The name of the method without namespace.</param>
        /// <param name="method">The type information for the method, if successful. Null otherwise.</param>
        // TODO: Lists of usable namespaces and modules, extended result type (failure reasons)
        bool GetMethod([NotNull] string name, [CanBeNull] out MethodDeclaration method);
    }
}
