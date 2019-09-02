using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class AckMessage : MessageBase
    {
        public AckMessage(Ulid? correlationId, SwimNode sourceNode) => (CorrelationId, SourceNode, MessageType) = (correlationId, sourceNode, MessageType.Ack);
    }
}
