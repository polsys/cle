﻿using System.Collections.Generic;
using Cle.Common.TypeSystem;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// Represents a method that has passed semantic analysis and can be emitted.
    /// Instances are mutable and can be transformed via optimizations.
    /// </summary>
    public class CompiledMethod
    {
        /// <summary>
        /// Gets the full name of this method.
        /// This can be used for debugging and emitting symbols.
        /// </summary>
        [NotNull]
        public string FullName { get; }

        /// <summary>
        /// Gets or sets the basic block graph for this method.
        /// </summary>
        [CanBeNull]
        public BasicBlockGraph Body { get; set; }

        /// <summary>
        /// Gets the list of local values for this method.
        /// This list may be modified using <see cref="AddLocal"/>.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyList<LocalValue> Values => _values;

        /// <summary>
        /// Gets the list of method calls for this method.
        /// This list may be modified using <see cref="AddCallInfo"/>.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyList<MethodCallInfo> CallInfos => _callInfos;

        private readonly List<LocalValue> _values = new List<LocalValue>();
        private readonly List<MethodCallInfo> _callInfos = new List<MethodCallInfo>();

        public CompiledMethod([NotNull] string fullName)
        {
            FullName = fullName;
        }

        /// <summary>
        /// Creates a new local value with the specified type and initial value, and returns its value index.
        /// The type of <paramref name="initialValue"/> is not checked against <paramref name="type"/>.
        /// </summary>
        public int AddLocal([NotNull] TypeDefinition type, ConstantValue initialValue)
        {
            _values.Add(new LocalValue(type, initialValue));
            return _values.Count - 1;
        }

        /// <summary>
        /// Creates and adds new <see cref="MethodCallInfo"/> to <see cref="CallInfos"/> and returns its index.
        /// </summary>
        /// <param name="calleeIndex">The body index of the called method.</param>
        /// <param name="parameterLocals">Local indices for the parameters.</param>
        /// <param name="calleeName">The full name of the called method, used for debugging.</param>
        public int AddCallInfo(int calleeIndex, [NotNull] int[] parameterLocals, [NotNull] string calleeName)
        {
            _callInfos.Add(new MethodCallInfo(calleeIndex, parameterLocals, calleeName));
            return _callInfos.Count - 1;
        }
    }
}
