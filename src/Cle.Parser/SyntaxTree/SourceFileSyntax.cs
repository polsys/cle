using System.Collections.Immutable;

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
        public string Namespace { get; }

        /// <summary>
        /// Gets the source file name.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets the global functions declared in this source file.
        /// </summary>
        public ImmutableList<FunctionSyntax> Functions { get; }

        public SourceFileSyntax(
            string namespaceName,
            string filename,
            ImmutableList<FunctionSyntax> functions)
        {
            Namespace = namespaceName;
            Filename = filename;
            Functions = functions;
        }
    }
}
