using System;
using System.Collections.Generic;
using System.Linq;
using Core.API.Consensus;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BlockGraphProto : IEquatable<BlockGraphProto>
    {
        public string Id { get; set; }
        public bool Included { get; set; }
        public bool Replied { get; set; }

        [ProtoMember(1)]
        public BlockIDProto Block = new BlockIDProto();

        [ProtoMember(2)]
        public List<DepProto> Deps = new List<DepProto>();

        [ProtoMember(3)]
        public BlockIDProto Prev = new BlockIDProto();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public BlockGraph ToBlockGraph()
        {
            BlockGraph blockGraph;

            if (Prev == null)
            {
                blockGraph = new BlockGraph(
                    new BlockID(Block.Hash, Block.Node, Block.Round, Block.SignedBlock));
            }
            else
            {
                blockGraph = new BlockGraph(
                    new BlockID(Block.Hash, Block.Node, Block.Round, Block.SignedBlock),
                    new BlockID(Prev.Hash, Prev.Node, Prev.Round, Prev.SignedBlock));
            }

            foreach (var dep in Deps)
            {
                var deps = new List<BlockID>();
                foreach (var d in dep.Deps)
                {
                    deps.Add(new BlockID(d.Hash, d.Node, d.Round, d.SignedBlock));
                }

                if (dep.Prev == null)
                {
                    blockGraph.Deps.Add(
                      new Dep(
                          new BlockID(dep.Block.Hash, dep.Block.Node, dep.Block.Round, dep.Block.SignedBlock), deps)
                    );
                }
                else
                {
                    blockGraph.Deps.Add(
                      new Dep(
                          new BlockID(dep.Block.Hash, dep.Block.Node, dep.Block.Round, dep.Block.SignedBlock), deps,
                          new BlockID(dep.Prev.Hash, dep.Prev.Node, dep.Prev.Round, dep.Prev.SignedBlock))
                    );
                }
            }

            return blockGraph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lookup"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IEnumerable<BlockGraphProto> NextBlockGraph(ILookup<string, BlockGraphProto> lookup, ulong node)
        {
            if (lookup == null)
                throw new ArgumentNullException(nameof(lookup));

            if (node < 0)
                throw new ArgumentOutOfRangeException(nameof(node));

            for (int i = 0, lookupCount = lookup.Count; i < lookupCount; i++)
            {
                var blockGraphs = lookup.ElementAt(i);
                BlockGraphProto root = null;

                var sorted = CurrentNodeFirst(blockGraphs.ToList(), node);

                foreach (var next in sorted)
                {
                    if (next.Block.Node.Equals(node))
                        root = NewBlockGraph(next);
                    else
                        AddDependency(root, next);
                }

                if (root == null)
                    continue;

                yield return root;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphs"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private static IEnumerable<BlockGraphProto> CurrentNodeFirst(List<BlockGraphProto> blockGraphs, ulong node)
        {
            // Not the best solution...
            var list = new List<BlockGraphProto>();
            var nodeIndex = blockGraphs.FindIndex(x => x.Block.Node.Equals(node));

            list.Add(blockGraphs[nodeIndex]);
            blockGraphs.RemoveAt(nodeIndex);
            list.AddRange(blockGraphs);

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        private static BlockGraphProto NewBlockGraph(BlockGraphProto next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            return new BlockGraphProto
            {
                Block = next.Block,
                Deps = next.Deps,
                Id = next.Id,
                Prev = next.Prev,
                Included = next.Included,
                Replied = next.Replied
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="next"></param>
        public static void AddDependency(BlockGraphProto root, BlockGraphProto next)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (root.Deps?.Any() != true)
            {
                root.Deps = new List<DepProto>();
            }

            root.Deps.Add(new DepProto
            {
                Id = next.Id,
                Block = next.Block,
                Deps = next.Deps?.Select(d => d.Block).ToList(),
                Prev = next.Prev ?? null
            });
        }

        public static bool operator ==(BlockGraphProto left, BlockGraphProto right) => Equals(left, right);

        public static bool operator !=(BlockGraphProto left, BlockGraphProto right) => !Equals(left, right);

        public override bool Equals(object obj) => (obj is BlockGraphProto blockGraph) && Equals(blockGraph);

        public bool Equals(BlockGraphProto other) => (Id, Block.Hash, Block.Node, Block.Round, Deps.Count) == (other.Id, other.Block.Hash, other.Block.Node, other.Block.Round, other.Deps.Count);

        public override int GetHashCode() => base.GetHashCode();
    }
}
