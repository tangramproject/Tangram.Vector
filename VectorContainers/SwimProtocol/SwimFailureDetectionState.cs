using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public enum SwimFailureDetectionState
    {
        Idle,
        Pinged,
        PrePingReq,
        PingReqed,
        Alive,
        Expired,
    }
}
