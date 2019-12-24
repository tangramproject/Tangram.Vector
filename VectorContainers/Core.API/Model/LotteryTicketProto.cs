using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class LotteryTicketProto
    {
        [ProtoMember(1)]
        public string SerialNumber { get; set; }
    }
}
