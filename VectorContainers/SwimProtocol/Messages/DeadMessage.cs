using Newtonsoft.Json;
using NUlid;
using SwimProtocol.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class DeadMessage : MessageBase, IHasSubject
    {
        public DeadMessage() { }

        [JsonProperty(PropertyName = "sjn")]
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode SubjectNode { get; set; }

        public DeadMessage(Ulid correlationId, ISwimNode sourceNode, ISwimNode subjectNode) => (CorrelationId, SourceNode, SubjectNode, MessageType) = (correlationId, sourceNode, subjectNode, MessageType.Dead);

        public override int GetMessageOverrideWeight(MessageBase message)
        {
            if (!(message is IHasSubject subject)) return 0;

            var s = subject.SubjectNode;

            if (s.Endpoint != SubjectNode.Endpoint) return 0;

            switch (message.MessageType)
            {
                case MessageType.Alive:
                case MessageType.Suspect:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
