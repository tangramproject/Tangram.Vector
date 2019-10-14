using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class ReplayMissingProto
    {
        public string Id { get; set; }
        [ProtoMember(1)]
        public ulong Node { get; set; }
        [ProtoMember(2)]
        public List<IEnumerable<BlockGraphProto>> Attempts { get; set; }
    }
}
