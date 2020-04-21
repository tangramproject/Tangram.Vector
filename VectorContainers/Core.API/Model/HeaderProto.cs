using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class HeaderProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public string BulletProof { get; set; }
        [ProtoMember(2)]
        public int Difficulty { get; set; }
        [ProtoMember(3)]
        public long Height { get; set; }
        [ProtoMember(4)]
        public int MinStake { get; set; }
        [ProtoMember(5)]
        public string Nonce { get; set; }
        [ProtoMember(6)]
        public string Proof { get; set; }
        [ProtoMember(7)]
        public string PrevNonce { get; set; }
        [ProtoMember(8)]
        public string PublicKey { get; set; }
        [ProtoMember(9)]
        public ulong Reward { get; set; }
        [ProtoMember(10)]
        public string Rnd { get; set; }
        [ProtoMember(11)]
        public string Seed { get; set; }
        [ProtoMember(12)]
        public string Security { get; set; }
        [ProtoMember(13)]
        public string Signature { get; set; }
        [ProtoMember(14)]
        public object TransactionModel { get; set; }
    }
}
