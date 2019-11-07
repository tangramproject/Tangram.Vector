using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class PayloadProto
    {
        [ProtoMember(1)]
        public string Agent { get; set; }
        [ProtoMember(2)]
        public byte[] Payload { get; set; }
        [ProtoMember(3)]
        public byte[] Signature { get; set; }
        [ProtoMember(4)]
        public byte[] PublicKey { get; set; }
    }
}
