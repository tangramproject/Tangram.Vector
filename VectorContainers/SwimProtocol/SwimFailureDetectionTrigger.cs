using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public enum SwimFailureDetectionTrigger
    {
        Ping,
        PingExpireLive,
        PingExpireNoResponse,
        PingReq,
        ProtocolExpireDead,
        ProtocolExpireLive,
        Reset,
    }
}
