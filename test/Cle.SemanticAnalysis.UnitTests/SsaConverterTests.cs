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

        [Test]
        public void Successive_basic_blocks()
        {
            const string source = @"
; #0   int32 param
BB_0:
    Multiply #0 * #0 -> #0

BB_1:
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

BB_1:
    Add #1 + #1 -> #2
    Return #2
";
            AssertDisassembly(result, expected);
        }

        [Test]
        public void Successive_basic_blocks_with_gap_in_between()
        {
            const string source = @"
; #0   int32 param
BB_0:
    Multiply #0 * #0 -> #0
    ==> BB_2

BB_2:
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
    ==> BB_2

BB_2:
    Add #1 + #1 -> #2
    Return #2
";
            AssertDisassembly(result, expected);
        }

        [Test]
        public void Branch_that_does_not_merge_back()
        {
            const string source = @"
; #0   int32 param
; #1   bool param
; #2   int32
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Multiply #0 * #0 -> #2
    Return #2

BB_2:
    Return #0
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);
            
            // No change expected
            AssertDisassembly(result, source);
        }

        [Test]
        public void Branch_that_merges_back_and_produces_phi()
        {
            // int32 f(int32 value, bool square)
            // {
            //     int32 result = value;
            //     if (square) { result = result * result; }
            //     return result;
            // }
            const string source = @"
; #0   int32 param
; #1   bool param
; #2   int32
; #3   int32
BB_0:
    CopyValue #0 -> #2
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Multiply #2 * #2 -> #3
    CopyValue #3 -> #2

BB_2:
    Return #2
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);

            const string expected = @"
; #0   int32 param
; #1   bool param
; #2   int32
; #3   int32
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Multiply #0 * #0 -> #2

BB_2:
    PHI (#0, #2) -> #3
    Return #3
";
            AssertDisassembly(result, expected);
        }

        [Test]
        public void Double_branch_that_merges_back_and_produces_phis_in_two_blocks()
        {
            // int32 f(int32 value, bool square, bool twice)
            // {
            //     int32 result = value;
            //     if (square) {
            //         result = result * result;
            //         if (twice) { result = result * result; }
            //     }
            //     return result;
            // }
            const string source = @"
; #0   int32 param
; #1   bool param
; #2   bool param
; #3   int32
; #4   int32
; #5   int32
BB_0:
    CopyValue #0 -> #3
    BranchIf #1 ==> BB_1
    ==> BB_4

BB_1:
    Multiply #3 * #3 -> #4
    CopyValue #4 -> #3
    BranchIf #2 ==> BB_2
    ==> BB_3

BB_2:
    Multiply #3 * #3 -> #5
    CopyValue #5 -> #3

BB_3:

BB_4:
    Return #3
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);

            const string expected = @"
; #0   int32 param
; #1   bool param
; #2   bool param
; #3   int32
; #4   int32
; #5   int32
; #6   int32
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_4

BB_1:
    Multiply #0 * #0 -> #3
    BranchIf #2 ==> BB_2
    ==> BB_3

BB_2:
    Multiply #3 * #3 -> #4

BB_3:
    PHI (#3, #4) -> #6

BB_4:
    PHI (#0, #6) -> #5
    Return #5
";
            AssertDisassembly(result, expected);
        }

        [Test]
        public void Branch_that_merges_back_three_way_and_produces_one_phi()
        {
            // int32 f(bool a, bool b)
            // {
            //     int32 result = 0;
            //     if (a & b) { result = 3; }
            //     else if (a) { result = 2; }
            //     return result;
            // }
            const string source = @"
; #0   bool param
; #1   bool param
; #2   int32
; #3   bool
; #4   int32
; #5   int32
BB_0:
    Load 0 -> #2
    BitwiseAnd #0 & #1 -> #3
    BranchIf #3 ==> BB_1
    ==> BB_2

BB_1:
    Load 3 -> #4
    CopyValue #4 -> #2
    ==> BB_4

BB_2:
    BranchIf #0 ==> BB_3
    ==> BB_4

BB_3:
    Load 2 -> #5
    CopyValue #5 -> #2

BB_4:
    Return #2
";
            var original = MethodAssembler.Assemble(source, "Test::Method");
            var result = new SsaConverter().ConvertToSsa(original);

            const string expected = @"
; #0   bool param
; #1   bool param
; #2   int32
; #3   bool
; #4   int32
; #5   int32
; #6   int32
BB_0:
    Load 0 -> #2
    BitwiseAnd #0 & #1 -> #3
    BranchIf #3 ==> BB_1
    ==> BB_2

BB_1:
    Load 3 -> #4
    ==> BB_4

BB_2:
    BranchIf #0 ==> BB_3
    ==> BB_4

BB_3:
    Load 2 -> #5

BB_4:
    PHI (#2, #5, #4) -> #6
    Return #6
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
