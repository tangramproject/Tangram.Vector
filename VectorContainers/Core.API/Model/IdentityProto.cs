using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class IdentityProto
    {
        [ProtoMember(1)]
        public ulong Client { get; set; }
        [ProtoMember(2)]
        public byte[] Nonce { get; set; }
        [ProtoMember(3)]
        public ulong Server { get; set; }
        [ProtoMember(5)]
        public long Timestamp { get; set; }
    }
}
