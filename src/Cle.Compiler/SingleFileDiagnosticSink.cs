﻿using System.Collections.Generic;
using Cle.Common;
namespace Cle.Compiler
{
    /// <summary>
    /// A diagnostic sink associated with a single file and module.
    /// Instances are reusable, and <see cref="Reset"/> must be called before using the instance.
    /// </summary>
    internal class SingleFileDiagnosticSink : IDiagnosticSink
    {
        /// <summary>
        /// Gets the list of diagnostics emitted.
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        private string _moduleName = string.Empty;
        private string _filename = string.Empty;
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public void Reset(string moduleName, string filename)
        {
            _moduleName = moduleName;
            _filename = filename;
            _diagnostics.Clear();
        }

        public void Add(DiagnosticCode code, TextPosition position)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, null, null));
        }

        public void Add(DiagnosticCode code, TextPosition position, string? actual)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, actual, null));
        }

        public void Add(DiagnosticCode code, TextPosition position, string? actual, string? expected)
        {
            _diagnostics.Add(new Diagnostic(code, position, _filename, _moduleName, actual, expected));
        }
    }
}
