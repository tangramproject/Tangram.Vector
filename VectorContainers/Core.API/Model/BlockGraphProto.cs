using System.Collections.Generic;
using Core.API.Consensus;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BlockGraphProto
    {
        [ProtoMember(1)]
        public BlockIDProto Block = new BlockIDProto();

        [ProtoMember(2)]
        public List<DepProto> Deps = new List<DepProto>();

        [ProtoMember(3)]
        public BlockIDProto Prev = new BlockIDProto();

        public BlockGraph ToBlockGraph()
        {
            var blockGraph = new BlockGraph(
                new BlockID(Block.Hash, Block.Node, Block.Round, Block.SignedBlock),
                new BlockID(Prev.Hash, Prev.Node, Prev.Round, Prev.SignedBlock));

            foreach (var dep in Deps)
            {
                var deps = new List<BlockID>();
                foreach (var d in dep.Deps)
                {
                    deps.Add(new BlockID(d.Hash, d.Node, d.Round, d.SignedBlock));
                }
                blockGraph.Deps.Add(
                    new Dep(
                        new BlockID(dep.Block.Hash, dep.Block.Node, dep.Block.Round, dep.Block.SignedBlock), deps,
                        new BlockID(dep.Prev.Hash, dep.Prev.Node, dep.Prev.Round, dep.Prev.SignedBlock)
                    )
                );
            }

            return blockGraph;
        }
    }
}
