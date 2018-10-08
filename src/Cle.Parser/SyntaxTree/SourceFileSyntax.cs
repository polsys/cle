using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// The root syntax tree node.
    /// </summary>
    public class SourceFileSyntax
    {
        /// <summary>
        /// Gets the namespace name declared in this source file.
        /// </summary>
        [NotNull]
        public string Namespace { get; }

        /// <summary>
        /// Gets the global functions declared in this source file.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public ImmutableList<FunctionSyntax> Functions { get; }

        public SourceFileSyntax(
            [NotNull] string namespaceName,
            [NotNull, ItemNotNull] ImmutableList<FunctionSyntax> functions)
        {
            Namespace = namespaceName;
            Functions = functions;
        }
    }
}
