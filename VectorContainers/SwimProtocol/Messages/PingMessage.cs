using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class PingMessage : MessageBase
    {
        public PingMessage() { }
        public PingMessage(Ulid correlationId) => (CorrelationId, MessageType) = (correlationId, MessageType.Ping);
    }
}
