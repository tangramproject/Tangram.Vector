using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class InterpretedProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public ulong Consumed { get; set; }
        [ProtoMember(2)]
        public ulong Round { get; set; }
    }
}
