using ProtoBuf;

namespace Core.API.Model
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
