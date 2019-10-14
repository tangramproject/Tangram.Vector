using System.Collections.Generic;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class DepProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public BlockIDProto Block = new BlockIDProto();
        [ProtoMember(2)]
        public List<BlockIDProto> Deps = new List<BlockIDProto>();
        [ProtoMember(3)]
        public BlockIDProto Prev = new BlockIDProto();
    }
}
