using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class RoundProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public ulong Round { get; set; }
    }
}
