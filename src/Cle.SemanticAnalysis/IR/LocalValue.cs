using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// Represents a variable, temporary or constant local to a method.
    /// </summary>
    public class LocalValue
    {
        /// <summary>
        /// Gets the type of this local.
        /// </summary>
        public TypeDefinition Type { get; }

        /// <summary>
        /// Gets additional information about this local.
        /// </summary>
        public LocalFlags Flags { get; }

        public LocalValue(TypeDefinition type, LocalFlags flags)
        {
            Type = type;
            Flags = flags;
        }
    }
}
