using Newtonsoft.Json;
using NUlid;
using SwimProtocol.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class AliveMessage : MessageBase
    {
        public AliveMessage() { }

        [JsonProperty(PropertyName = "sjn")]
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode SubjectNode { get; set; }

        public AliveMessage(Ulid correlationId, ISwimNode sourceNode, ISwimNode subjectNode) => (CorrelationId, SourceNode, SubjectNode, MessageType) = (correlationId, sourceNode, subjectNode, MessageType.Alive);
    }
}
