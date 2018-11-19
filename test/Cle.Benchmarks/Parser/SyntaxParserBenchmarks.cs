using System.Text;
using BenchmarkDotNet.Attributes;
using Cle.Parser;

namespace Cle.Benchmarks.Parser
{
    public class SyntaxParserBenchmarks
    {
        private readonly byte[] _source;

        public SyntaxParserBenchmarks()
        {
            _source = Encoding.UTF8.GetBytes(SourceString);
        }

        [Benchmark]
        public int ParseSingleFileWithoutErrors()
        {
            var diagnostics = new BenchmarkDiagnosticsSink();

            SyntaxParser.Parse(_source, "filename.cle", diagnostics);

            return diagnostics.DiagnosticCount;
        }

        // TODO: This benchmark should still be extended with more syntax elements once they exist.
        // TODO: The source should be long enough to minimize initialization overhead, and somewhat realistic.
        private const string SourceString = @"
namespace Test::Namespace::With::A::Long::Name;

public int32 IntegerExpression()
{
    return 42 * 28 + 35 - 1 + 32 / (--1);
}

public bool IsTrue()
{
    return true;
}

public bool IfStatements()
{
    if (2 * 21)
    {
        return true;
    }
    else if (2 * 21 + 1)
    {
        return true;
    }
    else
    {
        return false;
    }
}
";
    }
}
