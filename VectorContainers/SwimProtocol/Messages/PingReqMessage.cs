using Newtonsoft.Json;
using NUlid;
using SwimProtocol.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    class PingReqMessage : MessageBase
    {
        [JsonProperty(PropertyName = "sjn")]
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode SubjectNode { get; set; }

        public PingReqMessage() { }
        public PingReqMessage(Ulid correlationId, ISwimNode subjectNode, ISwimNode sourceNode) => (CorrelationId, SubjectNode, SourceNode, MessageType) = (correlationId, subjectNode, sourceNode, MessageType.PingReq);
    }
}
