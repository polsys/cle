using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Cle.Parser;
using Cle.SemanticAnalysis;
using Cle.SemanticAnalysis.IR;

namespace Cle.Benchmarks.SemanticAnalysis
{
    public class SsaConverterBenchmarks
    {
        private readonly CompiledMethod _smallMethod;
        private readonly CompiledMethod _largeMethod;

        public SsaConverterBenchmarks()
        {
            _smallMethod = ParseAndCompileSingleMethod(SmallSource);
            _largeMethod = ParseAndCompileSingleMethod(LargeSource);
        }

        [Benchmark]
        public CompiledMethod ConvertSmallMethod() => new SsaConverter().ConvertToSsa(_smallMethod);

        [Benchmark]
        public int ConvertLargeAndSmallMethod()
        {
            // This test reuses the SsaConverter instance to exercise caching
            var converter = new SsaConverter();
            var large = converter.ConvertToSsa(_largeMethod);
            var small = converter.ConvertToSsa(_smallMethod);

            return large.Values.Count + small.Values.Count;
        }

        private const string SmallSource = @"
namespace Bench;

private int32 Max(int32 a, int32 b)
{
    var int32 result = a;
    if (a < b)
    {
        result = b;
    }
    return result;
}";

        private const string LargeSource = @"
namespace Bench;

private int32 CollatzOnCollatz(int32 n)
{
    // How many steps until the sequence reaches 1?
    var int32 steps = 0;
    while (n != 1)
    {
        if (n % 2 == 0)
        {
            n = n / 2;
        }
        else
        {
            n = 3 * n + 1;
        }
        steps = steps + 1;
    }
    
    // And now, how many steps until the sequence (steps, ...) reaches 1?
    n = steps;
    steps = 0;
    while (n != 1)
    {
        if (n % 2 == 0)
        {
            n = n / 2;
        }
        else
        {
            n = 3 * n + 1;
        }
        steps = steps + 1;
    }
    
    return steps;
}";

        private CompiledMethod ParseAndCompileSingleMethod(string source)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            var diagnostics = new BenchmarkDiagnosticsSink();

            // Parse the source
            const string sourceFilename = "test.cle";
            var syntaxTree = SyntaxParser.Parse(sourceBytes.AsMemory(), sourceFilename, diagnostics);

            if (syntaxTree is null || syntaxTree.Functions.Count != 1)
            {
                throw new InvalidOperationException("Expected a single method");
            }

            // Compile the declaration
            var declarationProvider = new NullDeclarationProvider();
            var declaration = MethodDeclarationCompiler.Compile(syntaxTree.Functions[0],
                syntaxTree.Namespace, sourceFilename,
                0, declarationProvider, diagnostics);

            // Compile the method body
            var result = new MethodCompiler(declarationProvider, diagnostics)
                .CompileBody(syntaxTree.Functions[0], declaration!, syntaxTree.Namespace, sourceFilename);
            
            if (diagnostics.DiagnosticCount > 0)
            {
                throw new InvalidOperationException("Expected no diagnostics");
            }
            return result!;
        }
    }

    internal class NullDeclarationProvider : IDeclarationProvider
    {
        public IReadOnlyList<MethodDeclaration> GetMethodDeclarations(string methodName,
            IReadOnlyList<string> visibleNamespaces, string sourceFile)
        {
            return new MethodDeclaration[] { };
        }
    }
}
