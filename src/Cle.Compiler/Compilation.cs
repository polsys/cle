using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Cle.Common;
using Cle.SemanticAnalysis;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// The class that holds all type and method information within a compilation session.
    /// A single instance of this type may be used concurrently from multiple threads.
    /// This class handles synchronization unless otherwise noted.
    /// </summary>
    internal class Compilation : IDeclarationProvider
    {
        /// <summary>
        /// Gets the list of diagnostics associated with this compilation.
        /// <see cref="DiagnosticsLock"/> must be acquired prior to accessing this property.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Synchronization object for <see cref="Diagnostics"/>.
        /// </summary>
        [NotNull]
        public object DiagnosticsLock { get; } = new object();

        /// <summary>
        /// Gets whether any diagnostics are classified as errors.
        /// This value may be updated concurrently in sync with <see cref="Diagnostics"/>.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets the index of the method marked as the entry point.
        /// This value can be set only once, by <see cref="TrySetEntryPointIndex"/>.
        /// </summary>
        public int EntryPointIndex => _entryPointIndex;

        [NotNull]
        [ItemNotNull]
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        /// <summary>
        /// First level: namespace name.
        /// Second level: method name.
        /// Third level: list of methods with the name (because of private methods, may have more than one).
        /// </summary>
        [NotNull]
        private readonly Dictionary<string, Dictionary<string, List<MethodDeclaration>>> _methodDeclarations =
            new Dictionary<string, Dictionary<string, List<MethodDeclaration>>>();

        [NotNull]
        private readonly object _declarationLock = new object();

        [NotNull]
        private readonly IndexedRandomAccessStore<CompiledMethod> _methodBodies =
            new IndexedRandomAccessStore<CompiledMethod>();

        [NotNull]
        private readonly object _methodBodyLock = new object();
        private int _entryPointIndex = -1;

        /// <summary>
        /// Adds the given collection of diagnostics to <see cref="Diagnostics"/>.
        /// This function may be called from multiple threads.
        /// </summary>
        public void AddDiagnostics([NotNull] IReadOnlyList<Diagnostic> diagnosticsToAdd)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.AddRange(diagnosticsToAdd);

                // Update HasErrors
                foreach (var diagnostic in diagnosticsToAdd)
                {
                    if (diagnostic.IsError)
                        HasErrors = true;
                }
            }
        }

        /// <summary>
        /// Adds an error about missing source file.
        /// This function may be called from multiple threads.
        /// </summary>
        /// <param name="moduleName">The module that should contain the file.</param>
        /// <param name="filename">The name of the missing file.</param>
        public void AddMissingFileError([NotNull] string moduleName, [NotNull] string filename)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.Add(new Diagnostic(DiagnosticCode.SourceFileNotFound, default,
                    filename, moduleName, null, null));
                HasErrors = true;
            }
        }

        /// <summary>
        /// Adds a diagnostic specific to a single module.
        /// This function may be called from multiple threads.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="moduleName">The module that was not found.</param>
        public void AddModuleLevelDiagnostic(DiagnosticCode code, [NotNull] string moduleName)
        {
            lock (DiagnosticsLock)
            {
                _diagnostics.Add(new Diagnostic(code, default, null, moduleName, null, null));
                HasErrors = true;
            }
        }

        /// <summary>
        /// Adds the given method declaration to the compilation.
        /// Returns false if the name is already visible.
        /// This method may be called from multiple threads.
        /// </summary>
        /// <param name="methodName">The name of the method without namespace prefix.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <param name="declaration">The method declaration to be associated with the full method name.</param>
        // TODO: Modules
        public bool AddMethodDeclaration(
            [NotNull] string methodName,
            [NotNull] string namespaceName,
            [NotNull] MethodDeclaration declaration)
        {
            // TODO: This imposes a performance penalty, especially as adds typically happen in a batch.
            // TODO: Investigate when we have a realistic workload.
            lock (_declarationLock)
            {
                // The declaration dictionary is indexed by namespace name
                // TODO: Add a separate method for creating namespaces, then call that as part of pre-semantic analysis step
                // TODO: That way, we can assert that all namespace references are valid
                if (!_methodDeclarations.ContainsKey(namespaceName))
                {
                    _methodDeclarations.Add(namespaceName, new Dictionary<string, List<MethodDeclaration>>());
                }

                var nameMethodDict = _methodDeclarations[namespaceName];

                // Then the value is a dictionary of (method name, list of methods) pairs
                // TODO: Do we need to support overloading?
                if (nameMethodDict.TryGetValue(methodName, out var declarations))
                {
                    // We know that there are existing methods with the same name (private or not)
                    // If we're adding a non-private method, fail because the name would become ambiguous
                    if (declaration.Visibility != Visibility.Private)
                        return false;

                    // Now we know that the declaration is private
                    // If the name is used in the same file, or used for a non-private method, fail
                    foreach (var possibleConflict in declarations)
                    {
                        if (possibleConflict.Visibility != Visibility.Private ||
                            possibleConflict.DefiningFilename == declaration.DefiningFilename)
                            return false;
                    }

                    declarations.Add(declaration);
                    return true;
                }
                else
                {
                    // Use initial capacity of 1 because that's the common case
                    nameMethodDict.Add(methodName, new List<MethodDeclaration>(1) { declaration });
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns a list of matching method declarations.
        /// This method may be called from multiple threads.
        /// </summary>
        /// <param name="methodName">The name of the method without namespace prefix.</param>
        /// <param name="visibleNamespaces">Namespaces available for searching the method.</param>
        /// <param name="sourceFile">The current source file, used for matching private methods.</param>
        // TODO: Modules
        [NotNull, ItemNotNull]
        public IReadOnlyList<MethodDeclaration> GetMethodDeclarations(
            [NotNull] string methodName,
            [NotNull, ItemNotNull] IReadOnlyList<string> visibleNamespaces,
            [NotNull] string sourceFile)
        {
            var matchingMethods = ImmutableList<MethodDeclaration>.Empty;

            // TODO: Locking becomes unnecessary once no new methods are being added
            lock (_declarationLock)
            {
                // Go through all the namespace candidates and get matching methods
                foreach (var namespaceName in visibleNamespaces)
                {
                    // This should not happen as the language disallows referencing nonexistent namespaces
                    // TODO: Implement the said check and then change this to throw
                    // TODO: There is currently a test for this case; it should be removed as part of this
                    if (!_methodDeclarations.TryGetValue(namespaceName, out var methodDict))
                    {
                        continue;
                    }

                    // Go through the list of matching methods and add those that are visible (should be just one)
                    // TODO: Overloading?
                    if (methodDict.TryGetValue(methodName, out var declarations))
                    {
                        foreach (var decl in declarations)
                        {
                            if (decl.Visibility == Visibility.Private && decl.DefiningFilename != sourceFile)
                                continue;

                            matchingMethods = matchingMethods.Add(decl);
                        }
                    }
                }
            }

            return matchingMethods;
        }

        /// <summary>
        /// Gets a free method index that can be used for <see cref="SetMethodBody"/>.
        /// </summary>
        public int ReserveMethodSlot()
        {
            lock (_methodBodyLock)
            {
                return _methodBodies.ReserveIndex();
            }
        }

        /// <summary>
        /// Returns the method body associated with the given index.
        /// Throws if there is no method stored.
        /// </summary>
        [NotNull]
        public CompiledMethod GetMethodBody(int index)
        {
            try
            {
                lock (_methodBodyLock)
                {
                    return _methodBodies[index];
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "There is no method stored with the index.");
            }
        }

        /// <summary>
        /// Stores a method body to be accessed via the specified index.
        /// </summary>
        /// <param name="index">The method index from <see cref="ReserveMethodSlot"/>. Throws if this is not valid.</param>
        /// <param name="method">The method to store.</param>
        public void SetMethodBody(int index, [NotNull] CompiledMethod method)
        {
            try
            {
                lock (_methodBodyLock)
                {
                    _methodBodies[index] = method;
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "The index has not been reserved.");
            }
        }

        /// <summary>
        /// Sets the entry point index if not yet set.
        /// If the entry point is already set, returns false.
        /// This method performs necessary locking.
        /// </summary>
        /// <param name="index">The method index of the entry point.</param>
        public bool TrySetEntryPointIndex(int index)
        {
            // This can be done in a single atomic operation:
            // if the value is still the default, swap; if not, do nothing.
            return Interlocked.CompareExchange(ref _entryPointIndex, index, -1) == -1;
        }
    }
}
