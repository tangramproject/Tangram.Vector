using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class BlockProto
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public CoinProto Coin { get; set; }
        [ProtoMember(3)]
        public string PublicKey { get; set; }
        [ProtoMember(4)]
        public string Signature { get; set; }
    }
}
