using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    class PingReqMessage : MessageBase
    {
        public SwimNode Endpoint { get; }
        public PingReqMessage(Ulid? correlationId, SwimNode node, SwimNode sourceNode) => (CorrelationId, Endpoint, SourceNode, MessageType) = (correlationId, node, sourceNode, MessageType.PingReq);
    }
}
