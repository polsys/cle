using System.Collections.Generic;
using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// A diagnostic sink associated with a single file and module.
    /// </summary>
    internal class SingleFileDiagnosticSink : IDiagnosticSink
    {
        /// <summary>
        /// Gets the list of diagnostics emitted.
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        [NotNull]
        private readonly string _moduleName;

        [NotNull]
        private readonly string _filename;

        [NotNull, ItemNotNull]
        private readonly List<Diagnostic> _diagnostics;

        public SingleFileDiagnosticSink([NotNull] string moduleName, [NotNull] string filename)
        {
            _moduleName = moduleName;
            _filename = filename;
            _diagnostics = new List<Diagnostic>(0);
        }

        public void Add(DiagnosticCode code, TextPosition position)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, null, null));
        }

        public void Add(DiagnosticCode code, TextPosition position, string actual)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, actual, null));
        }

        public void Add(DiagnosticCode code, TextPosition position, string actual, string expected)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, actual, expected));
        }
    }
}
