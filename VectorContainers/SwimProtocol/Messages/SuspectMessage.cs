using Newtonsoft.Json;
using NUlid;
using SwimProtocol.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class SuspectMessage : MessageBase, IHasSubject
    {
        public SuspectMessage() { }

        [JsonProperty(PropertyName = "sjn")]
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode SubjectNode { get; set; }

        public SuspectMessage(Ulid correlationId, ISwimNode sourceNode, ISwimNode subjectNode) => (CorrelationId, SourceNode, SubjectNode, MessageType) = (correlationId, sourceNode, subjectNode, MessageType.Suspect);

        public override int GetMessageOverrideWeight(MessageBase message)
        {
            if (!(message is IHasSubject subject)) return 0;

            var s = subject.SubjectNode;

            if (s.Endpoint != SubjectNode.Endpoint) return 0;

            switch (message.MessageType)
            {
                case MessageType.Alive when message.CorrelationId.Value.Time > CorrelationId.Value.Time:
                    return -1;
                case MessageType.Alive:
                    return 1;
                case MessageType.Suspect when message.CorrelationId.Value.Time > CorrelationId.Value.Time:
                    return -1;
                case MessageType.Suspect:
                    return 1;
                case MessageType.Dead:
                    return -1;
                default:
                    return 0;
            }
        }
    }
}
