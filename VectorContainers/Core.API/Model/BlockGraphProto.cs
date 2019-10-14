using System;
using System.Collections.Generic;
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

        public static bool operator ==(BlockGraphProto left, BlockGraphProto right) => Equals(left, right);

        public static bool operator !=(BlockGraphProto left, BlockGraphProto right) => !Equals(left, right);

        public override bool Equals(object obj) => (obj is BlockGraphProto blockGraph) && Equals(blockGraph);

        public bool Equals(BlockGraphProto other) => (Id, Block.Hash, Block.Node, Block.Round, Deps.Count) == (other.Id, other.Block.Hash, other.Block.Node, other.Block.Round, other.Deps.Count);

        public override int GetHashCode() => base.GetHashCode();
    }
}
