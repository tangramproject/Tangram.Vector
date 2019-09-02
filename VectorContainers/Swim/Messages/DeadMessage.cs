using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class DeadMessage : MessageBase
    {
        public DeadMessage(SwimNode sourceNode) => (SourceNode, MessageType) = (sourceNode, MessageType.Dead);
    }
}
