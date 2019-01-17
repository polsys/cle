using System.Text;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class SsaConverterTests
    {
        [Test]
        public void Single_basic_block_with_same_variable_assigned_thrice()
        {
            // int32 a = 0;
            // a = 1;
            // a = 2;
            // return a;
            const string source = @"
; #0   int32
; #1   int32
; #2   int32
BB_0:
    Load 0 -> #0
    Load 1 -> #1
    CopyValue #1 -> #0
    Load 2 -> #2
    CopyValue #2 -> #0
    Return #0
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);

            const string expected = @"
; #0   int32
; #1   int32
; #2   int32
BB_0:
    Load 0 -> #0
    Load 1 -> #1
    Load 2 -> #2
    Return #2
";
            AssertDisassembly(result, expected);
        }

        [Test]
        public void Single_basic_block_with_already_ssa_arithmetic_on_parameters()
        {
            // int32 f(int32 a, int32 b)
            // {
            //     int32 c = a + b;
            //     return c - a;
            // }
            const string source = @"
; #0   int32 param
; #1   int32 param
; #2   int32
; #3   int32
BB_0:
    Add #0 + #1 -> #2
    Subtract #2 - #0 -> #3
    Return #3
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);
            
            // The source is already in SSA form, so no diff is expected
            AssertDisassembly(result, source);
        }

        [Test]
        public void Single_basic_block_with_reassignment_of_parameter()
        {
            // int32 TwiceParamSquared(int32 a)
            // {
            //     a = a * a;
            //     a = a + a;
            //     return a;
            // }
            const string source = @"
; #0   int32 param
BB_0:
    Multiply #0 * #0 -> #0
    Add #0 + #0 -> #0
    Return #0
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);

            const string expected = @"
; #0   int32 param
; #1   int32
; #2   int32
BB_0:
    Multiply #0 * #0 -> #1
    Add #1 + #1 -> #2
    Return #2
";
            AssertDisassembly(result, expected);
        }

        private void AssertDisassembly(CompiledMethod compiledMethod, string expected)
        {
            var builder = new StringBuilder();
            MethodDisassembler.Disassemble(compiledMethod, builder);

            Assert.That(builder.ToString().Trim().Replace("\r\n", "\n"),
                Is.EqualTo(expected.Trim().Replace("\r\n", "\n")));
        }
    }
}
