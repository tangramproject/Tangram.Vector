using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public enum MessageType : uint
    {
        Ping,
        Ack,
        Dead,
        Alive,
        PingReq,
        Composite
    }
}
