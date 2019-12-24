using ProtoBuf;

namespace Core.API.Model
{
    public enum LotteryStatus
    {
        Announced,
        Checked,
        Registered,
    }

    [ProtoContract]
    public class LotteryEventProto
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public LotteryStatus Status { get; set; }
        [ProtoMember(2)]
        public string EventName { get; set; }
        [ProtoMember(3)]
        public string IssueDate { get; set; }
        [ProtoMember(4)]
        public string DueDate { get; set; }
        [ProtoMember(5)]
        public string AnnouncementDate { get; set; }
        [ProtoMember(6)]
        public int NumOfRegistered { get; set; }
        [ProtoMember(7)]
        public string EventHash { get; set; }
    }
}
