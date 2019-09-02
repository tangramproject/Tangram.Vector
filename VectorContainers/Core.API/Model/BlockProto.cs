using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BlockProto
    {
        [ProtoMember(1)]
        public byte[] Key { get; set; }
        [ProtoMember(2)]
        public CoinProto Coin { get; set; }
        [ProtoMember(3)]
        public byte[] PublicKey { get; set; }
        [ProtoMember(4)]
        public byte[] Signature { get; set; }
    }
}
