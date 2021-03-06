using System;
using System.Collections.Immutable;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class BasicBlockGraphBuilderTests
    {
        [Test]
        public void Empty_graph_fails()
        {
            var graphBuilder = new BasicBlockGraphBuilder();

            Assert.That(() => graphBuilder.Build(), Throws.InvalidOperationException);
        }

        [Test]
        public void Graph_with_single_basic_block_succeeds()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(blockBuilder.Index, Is.EqualTo(0));

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions[0].Operation, Is.EqualTo(Opcode.Return));
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(-1));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(-1));
            Assert.That(graph.BasicBlocks[0].Predecessors, Is.Empty);
        }

        [Test]
        public void GetInitialBlockBuilder_cannot_be_called_twice()
        {
            var graphBuilder = new BasicBlockGraphBuilder();

            Assert.That(() => graphBuilder.GetInitialBlockBuilder(), Throws.Nothing);
            Assert.That(() => graphBuilder.GetInitialBlockBuilder(), Throws.InvalidOperationException);
        }

        [Test]
        public void Graph_with_two_consecutive_basic_blocks_succeeds()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();
            var secondBuilder = firstBuilder.CreateSuccessorBlock();
            secondBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(firstBuilder.Index, Is.EqualTo(0));
            Assert.That(secondBuilder.Index, Is.EqualTo(1));

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(2).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Is.Empty);
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(1));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(-1));
            Assert.That(graph.BasicBlocks[0].Predecessors, Is.Empty);

            Assert.That(graph.BasicBlocks[1].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[1].Instructions[0].Operation, Is.EqualTo(Opcode.Return));
            Assert.That(graph.BasicBlocks[1].DefaultSuccessor, Is.EqualTo(-1));
            Assert.That(graph.BasicBlocks[1].AlternativeSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[1].Predecessors);
        }

        [Test]
        public void CreateSuccessorBlock_cannot_be_called_twice()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();

            Assert.That(() => blockBuilder.CreateSuccessorBlock(), Throws.Nothing);
            Assert.That(() => blockBuilder.CreateSuccessorBlock(), Throws.InvalidOperationException);
        }

        [Test]
        public void CreateSuccessorBlock_does_not_set_successor_if_return_exists()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(blockBuilder.CreateSuccessorBlock(), Is.Not.Null);
            Assert.That(blockBuilder.DefaultSuccessor, Is.EqualTo(-1));
        }

        [Test]
        public void Graph_with_single_infinite_loop_succeeds()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.SetSuccessor(0);

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Is.Empty);
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(0));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[0].Predecessors);
        }

        [Test]
        public void GetBuilderByBlockIndex_returns_existent_builders()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var first = graphBuilder.GetInitialBlockBuilder();
            
            // Out-of-bounds indices are not tolerated
            Assert.That(() => graphBuilder.GetBuilderByBlockIndex(1), Throws.InstanceOf<ArgumentOutOfRangeException>());

            var second = graphBuilder.GetNewBasicBlock();

            Assert.That(graphBuilder.GetBuilderByBlockIndex(0), Is.SameAs(first));
            Assert.That(graphBuilder.GetBuilderByBlockIndex(1), Is.SameAs(second));
        }

        [Test]
        public void SetSuccessor_cannot_be_called_twice()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();

            Assert.That(() => blockBuilder.SetSuccessor(0), Throws.Nothing);
            Assert.That(blockBuilder.DefaultSuccessor, Is.EqualTo(0));
            Assert.That(() => blockBuilder.SetSuccessor(0), Throws.InvalidOperationException);
        }

        [Test]
        public void SetSuccessor_does_nothing_if_return_exists()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(() => blockBuilder.SetSuccessor(0), Throws.Nothing);
            Assert.That(blockBuilder.DefaultSuccessor, Is.EqualTo(-1));
        }

        [Test]
        public void SetAlternativeSuccessor_cannot_be_called_twice()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();

            Assert.That(() => blockBuilder.SetAlternativeSuccessor(0), Throws.Nothing);
            Assert.That(blockBuilder.AlternativeSuccessor, Is.EqualTo(0));
            Assert.That(() => blockBuilder.SetAlternativeSuccessor(0), Throws.InvalidOperationException);
        }

        [Test]
        public void SetAlternativeSuccessor_does_nothing_if_return_exists()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(() => blockBuilder.SetAlternativeSuccessor(0), Throws.Nothing);
            Assert.That(blockBuilder.AlternativeSuccessor, Is.EqualTo(-1));
        }

        [Test]
        public void Appending_to_block_after_adding_return_does_nothing()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            Assert.That(blockBuilder.Instructions, Has.Exactly(1).Items);

            blockBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            Assert.That(blockBuilder.Instructions, Has.Exactly(1).Items);
        }

        [Test]
        public void Block_cannot_be_appended_to_after_setting_successor()
        {
            var blockBuilder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            blockBuilder.SetSuccessor(0);
            
            blockBuilder.AppendInstruction(Opcode.Nop, 0, 0, 0);
            Assert.That(blockBuilder.Instructions, Is.Empty);
        }

        [TestCase(1)]
        [TestCase(-2)]
        public void Graph_with_out_of_bounds_default_successor_fails(int successor)
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.SetSuccessor(successor);

            Assert.That(() => graphBuilder.Build(), Throws.InvalidOperationException);
        }

        [Test]
        public void Graph_with_branch_and_merge_succeeds()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            var leftBuilder = firstBuilder.CreateSuccessorBlock();
            var rightBuilder = firstBuilder.CreateBranch(7);

            var finalBlock = leftBuilder.CreateSuccessorBlock();
            rightBuilder.SetSuccessor(leftBuilder.DefaultSuccessor);
            finalBlock.AppendInstruction(Opcode.Return, 0, 0, 0);

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(4).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions[0].Operation, Is.EqualTo(Opcode.BranchIf));
            Assert.That(graph.BasicBlocks[0].Instructions[0].Left, Is.EqualTo(7));
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(1));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(2));

            Assert.That(graph.BasicBlocks[1].Instructions, Is.Empty);
            Assert.That(graph.BasicBlocks[1].DefaultSuccessor, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[1].Predecessors);

            Assert.That(graph.BasicBlocks[2].Instructions, Is.Empty);
            Assert.That(graph.BasicBlocks[2].DefaultSuccessor, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[2].Predecessors);

            Assert.That(graph.BasicBlocks[3].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[3].Instructions[0].Operation, Is.EqualTo(Opcode.Return));
            Assert.That(graph.BasicBlocks[3].DefaultSuccessor, Is.EqualTo(-1));
            Assert.That(graph.BasicBlocks[3].AlternativeSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 1, 2 }, graph.BasicBlocks[3].Predecessors);
        }

        [Test]
        public void Graph_with_two_returning_branches_and_empty_block_succeeds()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            var leftBuilder = firstBuilder.CreateSuccessorBlock();
            leftBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);
            var rightBuilder = firstBuilder.CreateBranch(7);
            rightBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            // This block does not return nor is referenced, therefore it should be dropped
            var finalBuilder = leftBuilder.CreateSuccessorBlock();
            finalBuilder.AppendInstruction(Opcode.CopyValue, 1, 2, 3);

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(3).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions[0].Operation, Is.EqualTo(Opcode.BranchIf));
            Assert.That(graph.BasicBlocks[0].Instructions[0].Left, Is.EqualTo(7));
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(1));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(2));

            Assert.That(graph.BasicBlocks[1].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[1].DefaultSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[1].Predecessors);

            Assert.That(graph.BasicBlocks[2].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[2].DefaultSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[2].Predecessors);
        }

        [Test]
        public void Unreachable_blocks_are_dropped()
        {
            var graphBuilder = new BasicBlockGraphBuilder();

            // The first and the last block are alive, the two in the middle can be dropped
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();
            graphBuilder.GetNewBasicBlock().CreateSuccessorBlock();
            firstBuilder.CreateSuccessorBlock().AppendInstruction(Opcode.Return, 0, 0, 0);

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(2).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Is.Empty);
            Assert.That(graph.BasicBlocks[0].DefaultSuccessor, Is.EqualTo(1));
            Assert.That(graph.BasicBlocks[0].AlternativeSuccessor, Is.EqualTo(-1));

            Assert.That(graph.BasicBlocks[1].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[1].Instructions[0].Operation, Is.EqualTo(Opcode.Return));
            Assert.That(graph.BasicBlocks[1].DefaultSuccessor, Is.EqualTo(-1));
            CollectionAssert.AreEqual(new[] { 0 }, graph.BasicBlocks[1].Predecessors);
        }

        [Test]
        public void Phis_are_correctly_added()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AddPhi(4, ImmutableList<int>.Empty.AddRange(new[] { 17, 42 }));
            blockBuilder.AddPhi(3, ImmutableList<int>.Empty.AddRange(new[] { 6, 4 }));
            blockBuilder.AppendInstruction(Opcode.Return, 3, 0, 0);

            var graph = graphBuilder.Build();

            Assert.That(graph.BasicBlocks, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Instructions, Has.Exactly(1).Items);
            Assert.That(graph.BasicBlocks[0].Phis, Has.Exactly(2).Items);
            Assert.That(graph.BasicBlocks[0].Phis[0].Destination, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { 17, 42 }, graph.BasicBlocks[0].Phis[0].Operands);
            Assert.That(graph.BasicBlocks[0].Phis[1].Destination, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 6, 4 }, graph.BasicBlocks[0].Phis[1].Operands);
        }

        [Test]
        public void Graph_with_non_returning_branch_fails()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var firstBuilder = graphBuilder.GetInitialBlockBuilder();

            var leftBuilder = firstBuilder.CreateSuccessorBlock();
            leftBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            firstBuilder.CreateBranch(7); // This branch has no exit behavior

            Assert.That(() => graphBuilder.Build(), Throws.InvalidOperationException);
        }

        [Test]
        public void Branch_cannot_be_created_if_block_already_ends_in_branch()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            
            Assert.That(() => blockBuilder.CreateBranch(0), Throws.Nothing);
            Assert.That(() => blockBuilder.CreateBranch(0), Throws.InvalidOperationException);
        }

        [Test]
        public void Branch_cannot_be_created_if_block_ends_in_return()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(() => blockBuilder.CreateBranch(0), Throws.InvalidOperationException);
        }

        [Test]
        public void Branch_without_alternative_successor_is_invalid()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.AppendInstruction(Opcode.BranchIf, 0, 0, 0);
            blockBuilder.SetSuccessor(0);

            Assert.That(() => graphBuilder.Build(), Throws.InvalidOperationException);
        }

        [Test]
        public void Branch_without_default_successor_is_invalid()
        {
            var graphBuilder = new BasicBlockGraphBuilder();
            var blockBuilder = graphBuilder.GetInitialBlockBuilder();
            blockBuilder.CreateBranch(0).AppendInstruction(Opcode.Return, 0, 0, 0);

            Assert.That(() => graphBuilder.Build(), Throws.InvalidOperationException);
        }
    }
}
