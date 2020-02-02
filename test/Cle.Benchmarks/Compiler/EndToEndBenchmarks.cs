using BenchmarkDotNet.Attributes;
using Cle.Compiler;

namespace Cle.Benchmarks.Compiler
{
    public class EndToEndBenchmarks
    {
        private readonly SingleFileProvider _sourceProvider;
        private readonly NullOutputProvider _nullOutputProvider;
        
        public EndToEndBenchmarks()
        {
            _sourceProvider = new SingleFileProvider(Source);
            _nullOutputProvider = new NullOutputProvider();
        }

        [Benchmark]
        public int CompileSingleFileProgram()
        {
            var result = CompilerDriver.Compile(new CompilationOptions("."),
                _sourceProvider, _nullOutputProvider);

            return result.Diagnostics.Count;
        }

        private const string Source = @"
// This the Functional/CollatzOnCollatz integration test

namespace CollatzOnCollatz;

[EntryPoint]
public int32 Main()
{
    return Collatz(Collatz(9));
}

private int32 Collatz(int32 num)
{
    var int32 i = 0;
    while (num != 1)
    {
        i = i + 1;
        if (num % 2 == 0)
        {
            num = num / 2;
        }
        else
        {
            num = 3*num + 1;
        }
    }
    return i;
}
";
    }
}
