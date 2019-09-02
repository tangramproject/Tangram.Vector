using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class DeadMessage : MessageBase
    {
        public DeadMessage() { }
        public DeadMessage(Ulid correlationId, SwimNode sourceNode) => (CorrelationId, SourceNode, MessageType) = (correlationId, sourceNode, MessageType.Dead);
    }
}
