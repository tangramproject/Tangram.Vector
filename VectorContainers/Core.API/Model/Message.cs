using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Core.API.Model
{
    public class Message
    {
        public ulong MessageId { get; set; }
        public byte[] Address { get; set; }
        public byte[] Body { get; set; }
    }

    [ProtoContract]
    public class MessageProto
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public string Body { get; set; }
    }

    [ProtoContract]
    public class MessageProtoList
    {
        [ProtoMember(1)]
        public string Address { get; set; }
        [ProtoMember(2)]
        public List<Guid> Keys { get; set; }
    }
}
