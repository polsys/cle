using Cle.Common;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.Parser.UnitTests
{
    /// <summary>
    /// Provides fluent syntax for compilation diagnostic assertions.
    /// </summary>
    public class DiagnosticAssertResult
    {
        [NotNull]
        private readonly Diagnostic _diagnostic;

        public DiagnosticAssertResult([NotNull] Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        /// <summary>
        /// Asserts that <see cref="Diagnostic.Actual"/> for this diagnostic
        /// is equal to <paramref name="expectedActual"/>.
        /// </summary>
        public DiagnosticAssertResult WithActual([CanBeNull] string expectedActual)
        {
            Assert.That(_diagnostic.Actual, Is.EqualTo(expectedActual));

            return this;
        }

        /// <summary>
        /// Asserts that <see cref="Diagnostic.Actual"/> for this diagnostic is not null.
        /// </summary>
        public DiagnosticAssertResult WithNonNullActual()
        {
            Assert.That(_diagnostic.Actual, Is.Not.Null);

            return this;
        }
    }
}
