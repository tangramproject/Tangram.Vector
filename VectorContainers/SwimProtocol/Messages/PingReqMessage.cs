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
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode Endpoint { get; }
        public PingReqMessage() { }
        public PingReqMessage(Ulid correlationId, ISwimNode node, ISwimNode sourceNode) => (CorrelationId, Endpoint, SourceNode, MessageType) = (correlationId, node, sourceNode, MessageType.PingReq);
    }
}
