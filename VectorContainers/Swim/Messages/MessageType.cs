using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public enum MessageType
    {
        Ping,
        Ack,
        Dead,
        Alive,
        PingReq,
        Composite
    }
}
