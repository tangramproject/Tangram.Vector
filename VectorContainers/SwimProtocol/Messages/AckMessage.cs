using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class AckMessage : MessageBase
    {
        public AckMessage() { }
        public AckMessage(Ulid correlationId, ISwimNode sourceNode) => (CorrelationId, SourceNode, MessageType) = (correlationId, sourceNode, MessageType.Ack);
    }
}
