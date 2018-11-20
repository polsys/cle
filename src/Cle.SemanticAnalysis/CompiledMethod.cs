using System.Collections.Generic;
using Cle.Common.TypeSystem;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Represents a method that has passed semantic analysis and can be emitted.
    /// Instances are mutable and can be transformed via optimizations.
    /// </summary>
    public class CompiledMethod
    {
        /// <summary>
        /// Gets or sets the basic block graph for this method.
        /// </summary>
        public BasicBlockGraph Body { get; set; }

        /// <summary>
        /// Gets the list of local values for this method.
        /// This list may be modified using <see cref="AddTemporary"/> and other methods.
        /// </summary>
        public IReadOnlyList<LocalValue> Values => _values;

        private readonly List<LocalValue> _values = new List<LocalValue>();

        /// <summary>
        /// Creates a new local value with the specified type and initial value, and returns its value index.
        /// The type of <paramref name="initialValue"/> is not checked against <paramref name="type"/>.
        /// This method should be used for values that are not variables defined in the source code.
        /// </summary>
        public int AddTemporary([NotNull] TypeDefinition type, ConstantValue initialValue)
        {
            _values.Add(new LocalValue(type, initialValue));
            return _values.Count - 1;
        }
    }
}
