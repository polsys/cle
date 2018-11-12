using System.Collections.Generic;
using System.Linq;
using Cle.Common;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.UnitTests.Common
{
    public class TestingDiagnosticSink : IDiagnosticSink
    {
        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
        
        [NotNull]
        [ItemNotNull]
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public void Add(DiagnosticCode code, TextPosition position)
        {
            var diagnostic = new Diagnostic(code, position, string.Empty, string.Empty, null);

            _diagnostics.Add(diagnostic);
        }

        public void Add(DiagnosticCode code, TextPosition position, string actual)
        {
            var diagnostic = new Diagnostic(code, position, string.Empty, string.Empty, actual);

            _diagnostics.Add(diagnostic);
        }
        
        /// <summary>
        /// Asserts that there is a diagnostic with the specified code and source location.
        /// If the assertion succeeded, returns an result object for further fluent assertions.
        /// </summary>
        public DiagnosticAssertResult AssertDiagnosticAt(DiagnosticCode code, TextPosition position)
        {
            var diagnostic = Diagnostics.SingleOrDefault(x => x.Code == code && x.Position == position);
            Assert.That(diagnostic, Is.Not.Null, $"The diagnostic {code} did not exist at " +
                                                 $"({position.Line},{position.ByteInLine})");

            return new DiagnosticAssertResult(diagnostic);
        }

        /// <summary>
        /// Asserts that there is a diagnostic with the specified code and source location.
        /// If the assertion succeeded, returns an result object for further fluent assertions.
        /// </summary>
        public DiagnosticAssertResult AssertDiagnosticAt(DiagnosticCode code, int line, int offset)
        {
            var diagnostic = Diagnostics.SingleOrDefault(x =>
                x.Code == code && x.Position.Line == line && x.Position.ByteInLine == offset);
            Assert.That(diagnostic, Is.Not.Null, $"The diagnostic {code} did not exist at ({line},{offset})");

            return new DiagnosticAssertResult(diagnostic);
        }
    }
}
