// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMCore.Model
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
