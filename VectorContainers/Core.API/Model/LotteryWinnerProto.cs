using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class LotteryWinnerProto
    {
        [ProtoMember(1)]
        public long BlockHeight { get; set; }
        [ProtoMember(2)]
        public ulong Winner { get; set; }
        [ProtoMember(3)]
        public ulong[] Witnesses { get; set; }
        [ProtoMember(4)]
        public string Proof { get; set; }
        [ProtoMember(5)]
        public string Vrf { get; set; }
        [ProtoMember(6)]
        public string Message { get; set; }
        [ProtoMember(7)]
        public string PublicKey { get; set; }
        [ProtoMember(8)]
        public string Signature { get; set; }
    }
}
