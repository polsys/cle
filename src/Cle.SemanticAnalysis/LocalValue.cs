using Cle.Common.TypeSystem;

namespace Cle.SemanticAnalysis
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
        /// Gets the initial value of this local.
        /// </summary>
        public ConstantValue InitialValue { get; }

        public LocalValue(TypeDefinition type, ConstantValue initialValue)
        {
            Type = type;
            InitialValue = initialValue;
        }
    }
}
