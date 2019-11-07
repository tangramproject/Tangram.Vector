using Itc4net;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class StampProto
    {
        [ProtoMember(1)]
        public Stamp Stamp { get; set; }
    }
}
