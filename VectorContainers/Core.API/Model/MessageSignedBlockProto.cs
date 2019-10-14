using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class MessageSignedBlockProto
    {
        [ProtoMember(1)]
        public string Hash { get; set; }
        [ProtoMember(2)]
        public string PublicKey { get; set; }
        [ProtoMember(3)]
        public string Signature { get; set; }
    }
}
