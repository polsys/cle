using System.Collections.Immutable;
using System.Text;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class MethodDisassemblerTests
    {
        [Test]
        public void Disassemble_writes_both_values_and_basic_blocks()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().AppendInstruction(Opcode.Return, 0, 0, 0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };
            method.AddLocal(SimpleType.Int32, LocalFlags.None);
            method.AddLocal(SimpleType.Bool, LocalFlags.Parameter);

            const string expected = "; #0   int32\n" +
                                    "; #1   bool param\n" +
                                    "BB_0:\n" +
                                    "    Return #0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.Disassemble(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_single_basic_block_with_only_return()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().AppendInstruction(Opcode.Return, 2, 0, 0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            const string expected = "BB_0:\n    Return #2\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [TestCase(MethodCallType.Native, "")]
        [TestCase(MethodCallType.Imported, " import")]
        public void DisassembleBody_single_basic_block_with_call(MethodCallType callType, string expectedSuffix)
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Call, 0, 0, 1);
            blockBuilder.AppendInstruction(Opcode.Return, 2, 0, 0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };
            method.AddCallInfo(17, new[] { 3, 6, 9, 12 }, "Test::Callee", callType);

            string expected = "BB_0:\n" +
                              "    Call Test::Callee(#3, #6, #9, #12)" + expectedSuffix + " -> #1\n" +
                              "    Return #2\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_single_basic_block_with_phis()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AddPhi(7, ImmutableList<int>.Empty.Add(1));
            blockBuilder.AddPhi(12, ImmutableList<int>.Empty.AddRange(new[] { 8, 6, 4 }));
            blockBuilder.AppendInstruction(Opcode.Return, 2, 0, 0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            const string expected = "BB_0:\n" +
                                    "    PHI (#1) -> #7\n" +
                                    "    PHI (#8, #6, #4) -> #12\n" +
                                    "    Return #2\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_single_basic_block_with_several_instructions()
        {
            // TODO: Add new instructions to this test

            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Load, unchecked((ulong)-17), 0, 0);
            blockBuilder.AppendInstruction(Opcode.Load, 1, 0, 1);
            blockBuilder.AppendInstruction(Opcode.CopyValue, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Add, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Subtract, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Multiply, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Divide, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Modulo, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.ArithmeticNegate, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.ShiftLeft, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.ShiftRight, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.BitwiseAnd, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.BitwiseNot, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.BitwiseOr, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.BitwiseXor, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Less, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.LessOrEqual, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Equal, 1, 0, 2);
            blockBuilder.AppendInstruction(Opcode.Return, 2, 0, 0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };
            method.AddLocal(SimpleType.Int32, LocalFlags.None);
            method.AddLocal(SimpleType.Bool, LocalFlags.None);

            const string expected = "BB_0:\n" +
                                    "    Load -17 -> #0\n" +
                                    "    Load true -> #1\n" +
                                    "    CopyValue #1 -> #2\n" +
                                    "    Add #1 + #0 -> #2\n" +
                                    "    Subtract #1 - #0 -> #2\n" +
                                    "    Multiply #1 * #0 -> #2\n" +
                                    "    Divide #1 / #0 -> #2\n" +
                                    "    Modulo #1 % #0 -> #2\n" +
                                    "    ArithmeticNegate #1 -> #2\n" +
                                    "    ShiftLeft #1 << #0 -> #2\n" +
                                    "    ShiftRight #1 >> #0 -> #2\n" +
                                    "    BitwiseAnd #1 & #0 -> #2\n" +
                                    "    BitwiseNot #1 -> #2\n" +
                                    "    BitwiseOr #1 | #0 -> #2\n" +
                                    "    BitwiseXor #1 ^ #0 -> #2\n" +
                                    "    Less #1 < #0 -> #2\n" +
                                    "    LessOrEqual #1 <= #0 -> #2\n" +
                                    "    Equal #1 == #0 -> #2\n" +
                                    "    Return #2\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_handles_null_block()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var initialBlockBuilder = graphBuilder.GetInitialBlockBuilder();
            initialBlockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            initialBlockBuilder.CreateSuccessorBlock(); // No reference will be made and this will become null in Build()
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            const string expected = "BB_0:\n    Return #0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_single_basic_block_with_infinite_loop()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().SetSuccessor(0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            const string expected = "BB_0:\n    ==> BB_0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_two_basic_blocks_making_infinite_loop()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().CreateSuccessorBlock().SetSuccessor(0);
            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            // Fallthrough from BB_0 to BB_1 is not explicitly displayed
            const string expected = "BB_0:\n\nBB_1:\n    ==> BB_0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_three_basic_blocks_with_branch_1()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            // In this test case, the loop succeeds the initial block...
            var loopBuilder = firstBuilder.CreateSuccessorBlock();
            loopBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            loopBuilder.SetSuccessor(0);
            var returnBuilder = firstBuilder.CreateBranch(1);
            returnBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            // ...and the fallthrough from BB_0 to BB_1 is not explicitly displayed.
            const string expected = "BB_0:\n    BranchIf #1 ==> BB_2\n\n" +
                                    "BB_1:\n    Nop\n    ==> BB_0\n\n" +
                                    "BB_2:\n    Return #0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void DisassembleBody_three_basic_blocks_with_branch_2()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            // In this test case, the return succeeds the initial block...
            var returnBuilder = firstBuilder.CreateBranch(1);
            returnBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            var loopBuilder = firstBuilder.CreateSuccessorBlock();
            loopBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            loopBuilder.SetSuccessor(0);

            var method = new CompiledMethod("Test::Method") { Body = graphBuilder.Build() };

            // ...and both successors are displayed.
            const string expected = "BB_0:\n    BranchIf #1 ==> BB_1\n    ==> BB_2\n\n" +
                                    "BB_1:\n    Return #0\n\n" +
                                    "BB_2:\n    Nop\n    ==> BB_0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }
    }
}
