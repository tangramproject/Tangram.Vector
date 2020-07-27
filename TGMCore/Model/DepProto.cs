// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using ProtoBuf;

namespace TGMCore.Model
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
