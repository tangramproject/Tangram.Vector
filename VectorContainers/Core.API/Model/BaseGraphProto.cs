using System;
using System.Collections.Generic;
using System.Linq;
using Core.API.Consensus;
using Newtonsoft.Json;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BaseGraphProto<TAttach> : IEquatable<BaseGraphProto<TAttach>>, IBaseGraphProto<TAttach>
    {
        public string Id { get; set; }
        public bool Included { get; set; }
        public bool Replied { get; set; }

        [ProtoMember(1)]
        public BaseBlockIDProto<TAttach> Block { get; set; } = new BaseBlockIDProto<TAttach>();

        [ProtoMember(2)]
        public List<DepProto<TAttach>> Deps { get; set; } = new List<DepProto<TAttach>>();

        [ProtoMember(3)]
        public BaseBlockIDProto<TAttach> Prev { get; set; } = new BaseBlockIDProto<TAttach>();

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
        public static IEnumerable<BaseGraphProto<TAttach>> NextBlockGraph(ILookup<string, BaseGraphProto<TAttach>> lookup, ulong node)
        {
            if (lookup == null)
                throw new ArgumentNullException(nameof(lookup));

            if (node < 0)
                throw new ArgumentOutOfRangeException(nameof(node));

            for (int i = 0, lookupCount = lookup.Count; i < lookupCount; i++)
            {
                var blockGraphs = lookup.ElementAt(i);
                BaseGraphProto<TAttach> root = null;

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
        private static IEnumerable<BaseGraphProto<TAttach>> CurrentNodeFirst(List<BaseGraphProto<TAttach>> blockGraphs, ulong node)
        {
            // Not the best solution...
            var list = new List<BaseGraphProto<TAttach>>();
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
        private static BaseGraphProto<TAttach> NewBlockGraph(BaseGraphProto<TAttach> next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            return new BaseGraphProto<TAttach>
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
        public static void AddDependency(BaseGraphProto<TAttach> root, BaseGraphProto<TAttach> next)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (root.Deps?.Any() != true)
            {
                root.Deps = new List<DepProto<TAttach>>();
            }

            root.Deps.Add(new DepProto<TAttach>
            {
                Id = next.Id,
                Block = next.Block,
                Deps = next.Deps?.Select(d => d.Block).ToList(),
                Prev = next.Prev ?? null
            });
        }

        public static bool operator ==(BaseGraphProto<TAttach> left, BaseGraphProto<TAttach> right) => Equals(left, right);

        public static bool operator !=(BaseGraphProto<TAttach> left, BaseGraphProto<TAttach> right) => !Equals(left, right);

        public override bool Equals(object obj) => (obj is BaseGraphProto<TAttach> blockGraph) && Equals(blockGraph);

        public bool Equals(BaseGraphProto<TAttach> other)
        {
            return (Id, Block.Hash, Block.Node, Block.Round, Deps.Count) == (other.Id, other.Block.Hash, other.Block.Node, other.Block.Round, other.Deps.Count);
        }

        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <returns></returns>
        public T Cast<T>()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
