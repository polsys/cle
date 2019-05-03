using Cle.Common;
using NUnit.Framework;

namespace Cle.UnitTests.Common
{
    /// <summary>
    /// Provides fluent syntax for compilation diagnostic assertions.
    /// </summary>
    public class DiagnosticAssertResult
    {
        private readonly Diagnostic _diagnostic;

        public DiagnosticAssertResult(Diagnostic diagnostic)
        {
            _diagnostic = diagnostic;
        }

        /// <summary>
        /// Asserts that <see cref="Diagnostic.Actual"/> for this diagnostic
        /// is equal to <paramref name="expectedActual"/>.
        /// </summary>
        public DiagnosticAssertResult WithActual(string? expectedActual)
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

        /// <summary>
        /// Asserts that <see cref="Diagnostic.Expected"/> for this diagnostic
        /// is equal to <paramref name="expectedExpected"/>.
        /// </summary>
        public DiagnosticAssertResult WithExpected(string? expectedExpected)
        {
            Assert.That(_diagnostic.Expected, Is.EqualTo(expectedExpected));

            return this;
        }
    }
}
