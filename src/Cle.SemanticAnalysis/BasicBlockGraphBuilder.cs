using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// A mutable builder for <see cref="BasicBlockGraph"/> instances.
    /// </summary>
    internal class BasicBlockGraphBuilder
    {
        [NotNull]
        [ItemNotNull]
        private readonly List<BasicBlockBuilder> _builders = new List<BasicBlockBuilder>();

        /// <summary>
        /// Gets a builder for the initial basic block.
        /// This method can only be called once.
        /// </summary>
        public BasicBlockBuilder GetInitialBlockBuilder()
        {
            if (_builders.Count > 0)
                throw new InvalidOperationException("This method can only be called once.");

            return GetNewBasicBlock().builder;
        }
        
        /// <summary>
        /// Creates and returns a new basic block.
        /// </summary>
        public (BasicBlockBuilder builder, int index) GetNewBasicBlock()
        {
            var builder = new BasicBlockBuilder(this);
            _builders.Add(builder);

            return (builder, _builders.Count - 1);
        }

        /// <summary>
        /// Returns the completed immutable basic block graph.
        /// If the graph is invalid, throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public BasicBlockGraph Build()
        {
            // An empty graph is considered invalid - there must be at least one basic block
            if (_builders.Count == 0)
                throw new InvalidOperationException("The basic block graph is empty.");

            // Construct each basic block
            var blocks = ImmutableList<BasicBlock>.Empty.ToBuilder();
            foreach (var blockBuilder in _builders)
            {
                if (!blockBuilder.HasDefinedExitBehavior)
                {
                    throw new InvalidOperationException("A basic block has undefined exit behavior.");
                }
                if (blockBuilder.DefaultSuccessor >= _builders.Count ||
                    blockBuilder.AlternativeSuccessor >= _builders.Count)
                {
                    throw new InvalidOperationException("A basic block references a successor that does not exist.");
                }

                blocks.Add(new BasicBlock(
                    blockBuilder.Instructions.ToImmutable(),
                    blockBuilder.DefaultSuccessor,
                    blockBuilder.AlternativeSuccessor));
            }

            return new BasicBlockGraph(blocks.ToImmutable());
        }
    }
}
