using System.Text;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class MethodDisassemblerTests
    {
        [Test]
        public void Single_basic_block_with_only_return()
        {
            var method = new CompiledMethod();
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().AppendInstruction(Opcode.Return, 2, 0, 0);
            method.Body = graphBuilder.Build();

            const string expected = "BB_0:\n    Return #2\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void Single_basic_block_with_infinite_loop()
        {
            var method = new CompiledMethod();
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().SetSuccessor(0);
            method.Body = graphBuilder.Build();

            const string expected = "BB_0:\n    ==> BB_0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void Two_basic_blocks_making_infinite_loop()
        {
            var method = new CompiledMethod();
            var graphBuilder = new BasicBlockGraphBuilder();
            graphBuilder.GetInitialBlockBuilder().CreateSuccessorBlock().SetSuccessor(0);
            method.Body = graphBuilder.Build();

            // Fallthrough from BB_0 to BB_1 is not explicitly displayed
            const string expected = "BB_0:\n\nBB_1:\n    ==> BB_0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void Three_basic_blocks_with_branch_1()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            // In this test case, the loop succeeds the initial block...
            var loopBuilder = firstBuilder.CreateSuccessorBlock();
            loopBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            loopBuilder.SetSuccessor(0);
            var returnBuilder = firstBuilder.CreateBranch(1);
            returnBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            var method = new CompiledMethod();
            method.Body = graphBuilder.Build();

            // ...and the fallthrough from BB_0 to BB_1 is not explicitly displayed.
            const string expected = "BB_0:\n    BranchIf #1 ==> BB_2\n\n" +
                                    "BB_1:\n    Nop\n    ==> BB_0\n\n" +
                                    "BB_2:\n    Return #0\n\n";

            var builder = new StringBuilder();
            MethodDisassembler.DisassembleBody(method, builder);
            Assert.That(builder.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
        }

        [Test]
        public void Three_basic_blocks_with_branch_2()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            // In this test case, the return succeeds the initial block...
            var returnBuilder = firstBuilder.CreateBranch(1);
            returnBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            var loopBuilder = firstBuilder.CreateSuccessorBlock();
            loopBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            loopBuilder.SetSuccessor(0);

            var method = new CompiledMethod();
            method.Body = graphBuilder.Build();

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
