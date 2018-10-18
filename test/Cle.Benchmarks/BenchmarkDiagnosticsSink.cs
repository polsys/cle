using Cle.Common;

namespace Cle.Benchmarks
{
    internal class BenchmarkDiagnosticsSink : IDiagnosticSink
    {
        public int DiagnosticCount { get; private set; }

        public void Add(DiagnosticCode code, TextPosition position)
        {
            DiagnosticCount++;
        }

        public void Add(DiagnosticCode code, TextPosition position, string actual)
        {
            DiagnosticCount++;
        }
    }
}
