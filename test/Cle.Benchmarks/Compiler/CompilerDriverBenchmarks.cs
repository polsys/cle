using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Cle.Compiler;

namespace Cle.Benchmarks.Compiler
{
    public class CompilerDriverBenchmarks
    {
        private readonly SingleFileProvider _almostSemanticallyValidSourceProvider;
        private readonly NullOutputProvider _nullOutputProvider;

        public CompilerDriverBenchmarks()
        {
            _almostSemanticallyValidSourceProvider = new SingleFileProvider(AlmostSemanticallyValidSource);
            _nullOutputProvider = new NullOutputProvider();
        }

        [Benchmark]
        public int CompileSingleFileWithSemanticError()
        {
            var result = CompilerDriver.Compile(new CompilationOptions("."),
                _almostSemanticallyValidSourceProvider, _nullOutputProvider);
            return result.Diagnostics.Count;
        }
        
        // TODO: This should be extended with more syntactic elements once they exist
        // Two errors:
        //   - no function annotated with [EntryPoint] (TODO once implemented)
        //   - the last function has a type mismatch
        private const string AlmostSemanticallyValidSource = @"
namespace Benchmarks::Compiler::AlmostValidFile;

public int32 GetTheAnswerToEverything()
{
    bool isTrue = true;
    if (isTrue) { return 42; }
    else { return 41; }
}

public bool SwapBoolsAround()
{
    bool one = true;
    bool two = true;
    if (one) { two = false; }
    bool three = two;
    if (three) { return one; } else { return two; }
}

private bool TypeMismatch()
{
    return 0;
}
";

        private class SingleFileProvider : ISourceFileProvider
        {
            private static readonly string[] s_filenames = { "file.cle" };
            private readonly Memory<byte> _source;

            public SingleFileProvider(string source)
            {
                _source = Encoding.UTF8.GetBytes(source).AsMemory();
            }

            public bool TryGetFilenamesForModule(string moduleName, out IEnumerable<string> filenames)
            {
                filenames = s_filenames;
                return true;
            }

            public bool TryGetSourceFile(string filename, out Memory<byte> fileBytes)
            {
                if (filename == "file.cle")
                {
                    fileBytes = _source;
                    return true;
                }
                else
                {
                    fileBytes = Memory<byte>.Empty;
                    return false;
                }
            }
        }

        private class NullOutputProvider : IOutputFileProvider
        {
            public Stream GetExecutableStream()
            {
                return null;
            }

            public TextWriter GetDisassemblyWriter()
            {
                return null;
            }

            public TextWriter GetDebugFileWriter()
            {
                return null;
            }
        }
    }
}
