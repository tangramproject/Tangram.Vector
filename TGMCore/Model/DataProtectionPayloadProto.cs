// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace TGMCore.Model
{
    [ProtoContract]
    public class DataProtectionPayloadProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public string FriendlyName { get; set; }
        [ProtoMember(2)]
        public string Payload { get; set; }
    }
}
