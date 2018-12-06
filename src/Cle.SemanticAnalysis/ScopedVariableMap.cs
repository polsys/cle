using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// A scoped mapping of variable names to local value numbers.
    /// </summary>
    internal class ScopedVariableMap
    {
        // Benchmarking indicated that a dictionary is significantly faster than list
        // for large methods and not too much slower for tiny methods.
        // See ScopedVariableMapBenchmarks for test cases.
        private readonly Stack<Dictionary<string, int>> _scopeStack = new Stack<Dictionary<string, int>>();

        // A cache for scopes, as this instance should be reused across multiple methods.
        // This reduces GC pressure significantly.
        private readonly Stack<Dictionary<string, int>> _freeScopeCache = new Stack<Dictionary<string, int>>();
        private const int MaxCacheSize = 8;

        /// <summary>
        /// Creates a new empty variable scope and pushes it onto the scope stack.
        /// </summary>
        public void PushScope()
        {
            // Try to get a cached scope if possible
            _scopeStack.Push(_freeScopeCache.Count > 0 ? _freeScopeCache.Pop() : new Dictionary<string, int>());
        }

        /// <summary>
        /// Removes the topmost variable scope on the scope stack.
        /// </summary>
        public void PopScope()
        {
            if (_scopeStack.Count == 0)
                throw new InvalidOperationException("Scope stack is empty");

            // Pop the scope and cache it for next use (unless the cache is already full)
            var popped = _scopeStack.Pop();
            if (_freeScopeCache.Count < MaxCacheSize)
            {
                popped.Clear();
                _freeScopeCache.Push(popped);
            }
        }

        /// <summary>
        /// Adds the given variable to the current scope.
        /// Returns false if the name already exists in any scope.
        /// Throws if there is no scope on the stack.
        /// </summary>
        public bool TryAddVariable([NotNull] string name, int localIndex)
        {
            if (_scopeStack.Count == 0)
                throw new InvalidOperationException("Scope stack is empty");

            // Is the name already in use?
            if (TryGetVariable(name, out _))
            {
                return false;
            }

            // It was not - add the variable to the current scope
            _scopeStack.Peek().Add(name, localIndex);
            return true;
        }

        /// <summary>
        /// Gets the local index of the given variable.
        /// Returns false if the variable does not exist in any scope.
        /// Throws if there is no scope on the stack.
        /// </summary>
        public bool TryGetVariable([NotNull] string name, out int localIndex)
        {
            if (_scopeStack.Count == 0)
                throw new InvalidOperationException("Scope stack is empty");

            // Go through each scope and try to find the variable.
            // TryAddVariable guarantees that there are no duplicates.
            foreach (var scope in _scopeStack)
            {
                if (scope.TryGetValue(name, out localIndex))
                {
                    return true;
                }
            }

            // No match
            localIndex = -1;
            return false;
        }
    }
}
