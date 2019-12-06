using System.Collections.Generic;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class DepProto<TAttach>
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public BaseBlockIDProto<TAttach> Block = new BaseBlockIDProto<TAttach>();
        [ProtoMember(2)]
        public List<BaseBlockIDProto<TAttach>> Deps = new List<BaseBlockIDProto<TAttach>>();
        [ProtoMember(3)]
        public BaseBlockIDProto<TAttach> Prev = new BaseBlockIDProto<TAttach>();
    }
}
