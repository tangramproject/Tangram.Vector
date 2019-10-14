using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BlockInfoProto
    {
        [ProtoMember(1)]
        public string Hash { get; set; }
        [ProtoMember(2)]
        public ulong Node { get; set; }
        [ProtoMember(3)]
        public ulong Round { get; set; }
    }
}
