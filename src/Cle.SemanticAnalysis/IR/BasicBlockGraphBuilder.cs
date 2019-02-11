using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// A mutable builder for <see cref="BasicBlockGraph"/> instances.
    /// Use <see cref="GetInitialBlockBuilder"/> to get the first basic block builder, then use methods
    /// on block builders to add instructions and create new blocks.
    /// </summary>
    public class BasicBlockGraphBuilder
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

            return GetNewBasicBlock();
        }
        
        /// <summary>
        /// Creates and returns a new basic block builder.
        /// </summary>
        public BasicBlockBuilder GetNewBasicBlock()
        {
            var builder = new BasicBlockBuilder(this, _builders.Count);
            _builders.Add(builder);

            return builder;
        }

        /// <summary>
        /// Gets the basic block builder associated with the specified index.
        /// The builder must already be created.
        /// </summary>
        [NotNull]
        public BasicBlockBuilder GetBuilderByBlockIndex(int blockIndex)
        {
            return _builders[blockIndex];
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

            // Do a marking pass to skip basic blocks with no inbound edges
            // Also collect the predecessors of each block
            var liveBlocks = new bool[_builders.Count];
            var predecessors = new ImmutableList<int>[_builders.Count];
            for (var i = 0; i < predecessors.Length; i++)
            {
                predecessors[i] = ImmutableList<int>.Empty;
            }
            MarkBlock(0, liveBlocks, predecessors);

            // Construct each marked basic block
            var blocks = ImmutableList<BasicBlock>.Empty.ToBuilder();
            for (var i = 0; i < _builders.Count; i++)
            {
                // Skip unreachable blocks but append null to preserve indexing
                // Indexing can be fixed by an optimization pass after possible block merges etc.
                if (!liveBlocks[i])
                {
                    blocks.Add(null);
                    continue;
                }

                var blockBuilder = _builders[i];
                if (!blockBuilder.HasDefinedExitBehavior)
                {
                    throw new InvalidOperationException("A basic block has undefined exit behavior.");
                }

                blocks.Add(new BasicBlock(
                    blockBuilder.Instructions.ToImmutable(),
                    blockBuilder.Phis.ToImmutable(),
                    blockBuilder.DefaultSuccessor,
                    blockBuilder.AlternativeSuccessor,
                    predecessors[i]));
            }

            return new BasicBlockGraph(blocks.ToImmutable());
        }

        private void MarkBlock(int blockIndex, bool[] liveBlocks, ImmutableList<int>[] predecessors)
        {
            // If this block has already been visited, continue
            if (liveBlocks[blockIndex])
                return;

            liveBlocks[blockIndex] = true;

            // Recurse into outbound edges
            var block = _builders[blockIndex];
            if (block.DefaultSuccessor >= _builders.Count ||
                block.AlternativeSuccessor >= _builders.Count)
            {
                throw new InvalidOperationException("A basic block references a successor that does not exist.");
            }

            if (block.DefaultSuccessor >= 0)
            {
                predecessors[block.DefaultSuccessor] = predecessors[block.DefaultSuccessor].Add(blockIndex);
                MarkBlock(block.DefaultSuccessor, liveBlocks, predecessors);
            }
            if (block.AlternativeSuccessor >= 0)
            {
                predecessors[block.AlternativeSuccessor] = predecessors[block.AlternativeSuccessor].Add(blockIndex);
                MarkBlock(block.AlternativeSuccessor, liveBlocks, predecessors);
            }
        }
    }
}
