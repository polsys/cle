using System.Collections.Generic;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// This interface is used by <see cref="ExpressionCompiler"/> to resolve variable and method names.
    /// </summary>
    public interface INameResolver
    {
        /// <summary>
        /// Gets all matching method declarations for the given method name.
        /// </summary>
        /// <param name="name">Either a full or simple method name.</param>
        [NotNull, ItemNotNull]
        IReadOnlyList<MethodDeclaration> ResolveMethod([NotNull] string name);

        /// <summary>
        /// Tries to get the local index for the given variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="localIndex">If the variable was found, its local index.</param>
        bool TryResolveVariable([NotNull] string name, out int localIndex);
    }
}
