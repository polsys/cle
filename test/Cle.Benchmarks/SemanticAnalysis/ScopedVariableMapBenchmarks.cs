using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Cle.SemanticAnalysis;

namespace Cle.Benchmarks.SemanticAnalysis
{
    public class ScopedVariableMapBenchmarks
    {
        private readonly string[] _variableNames;
        private const int ScopeCount = 5;
        private const int VariablesPerScope = 7;

        public ScopedVariableMapBenchmarks()
        {
            _variableNames = new string[ScopeCount * VariablesPerScope];
            for (var i = 0; i < _variableNames.Length; i++)
            {
                _variableNames[i] = Convert.ToBase64String(Encoding.UTF8.GetBytes(i.ToString()));
            }
        }

        [Benchmark]
        public int SmallMethod()
        {
            // This is based on NameParsing.IsDigit
            var map = new ScopedVariableMap();
            var sum = 0;

            // Execute the scenario a few times to simulate the map being reused
            for (var i = 0; i < 5; i++)
            {
                map.PushScope();
                map.TryAddVariable("ch", 1);
                map.TryGetVariable("ch", out var a);
                map.TryGetVariable("ch", out var b);
                map.PopScope();

                sum += a + b;
            }

            return sum;
        }

        [Benchmark]
        public int LargerMethod()
        {
            // This is a synthetic benchmark, aiming to still be somewhat realistic
            var map = new ScopedVariableMap();
            var sum = 0;

            // Execute the scenario a few times to simulate the map being reused
            for (var iteration = 0; iteration < 5; iteration++)
            {
                // Create a lot of nested scopes and add variables to them
                for (var i = 0; i < ScopeCount; i++)
                {
                    map.PushScope();
                    for (var j = 0; j < VariablesPerScope; j++)
                    {
                        var index = i * ScopeCount + j;
                        map.TryAddVariable(_variableNames[index], index);
                    }
                }

                // Go through all the variables
                for (var i = 0; i < ScopeCount * VariablesPerScope; i++)
                {
                    // Do not go through them in exact order
                    map.TryGetVariable(_variableNames[i * 3 % (ScopeCount * VariablesPerScope)], out var index);
                    sum += index;
                }

                // Remove the scopes
                for (var i = 0; i < ScopeCount; i++)
                {
                    map.PopScope();
                }
            }

            return sum;
        }
    }
}
