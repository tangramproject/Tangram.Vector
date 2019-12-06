using System.Collections.Generic;
using ProtoBuf;

namespace Core.API.Model
{
    [ProtoContract]
    public class JobProto<TAttach>
    {
        public string Id { get; set; }

        [ProtoMember(1)]
        public string Hash { get; set; }
        [ProtoMember(2)]
        public ulong Node { get; set; }
        [ProtoMember(3)]
        public List<ulong> Nodes { get; set; }
        [ProtoMember(4)]
        public List<ulong> WaitingOn { get; set; }
        [ProtoMember(5)]
        public int TotalNodes { get; set; }
        [ProtoMember(6)]
        public int ExpectedTotalNodes { get; set; }
        [ProtoMember(7)]
        public JobState Status { get; set; }
        [ProtoMember(8)]
        public BaseGraphProto<TAttach> Model { get; set; }
        [ProtoMember(9)]
        public long Epoch { get; set; }
    }
}
