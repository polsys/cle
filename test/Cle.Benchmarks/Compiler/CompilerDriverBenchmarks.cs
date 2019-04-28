using BenchmarkDotNet.Attributes;
using Cle.Compiler;

namespace Cle.Benchmarks.Compiler
{
    public class CompilerDriverBenchmarks
    {
        private readonly SingleFileProvider _invalidSourceProvider;
        private readonly NullOutputProvider _nullOutputProvider;

        public CompilerDriverBenchmarks()
        {
            _invalidSourceProvider = new SingleFileProvider(SemanticallyInvalidSource);
            _nullOutputProvider = new NullOutputProvider();
        }

        [Benchmark]
        public int CompileSingleFileWithSemanticError()
        {
            var result = CompilerDriver.Compile(new CompilationOptions("."),
                _invalidSourceProvider, _nullOutputProvider);
            return result.Diagnostics.Count;
        }
        
        // Two errors:
        //   - no function annotated with [EntryPoint]
        //   - the function has a type mismatch
        private const string SemanticallyInvalidSource = @"
namespace InvalidFile;

private bool TypeMismatch()
{
    return 0;
}
";
    }
}
