using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class PingMessage : MessageBase
    {
        public PingMessage(Ulid? correlationId) => (CorrelationId, MessageType) = (correlationId, MessageType.Ping);
    }
}
