using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Cle.Parser;

namespace Cle.Benchmarks.Parser
{
    public class LexerBenchmarks
    {
        private readonly Memory<byte> _tokenStringBytes;

        public LexerBenchmarks()
        {
            _tokenStringBytes = Encoding.UTF8.GetBytes(StringOfTokens).AsMemory();
        }

        [Benchmark]
        public int ClassifyTokens()
        {
            var tokenCount = 0;
            var lexer = new Lexer(_tokenStringBytes);

            while (lexer.GetTokenType() != TokenType.EndOfFile)
            {
                tokenCount++;
            }

            return tokenCount;
        }
        
        private const string StringOfTokens = @"
namespace Test;
public         int32 Name(){
{{
1234 identifier + false £invalid -
if (25 > 4^5 && 1 != 5 | 6) { return true; }
}}}
";
    }
}
