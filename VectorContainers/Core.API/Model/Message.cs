using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class MessageProto
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public string Body { get; set; }
    }
}
