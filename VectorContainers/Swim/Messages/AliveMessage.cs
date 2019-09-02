using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class AliveMessage : MessageBase
    {
        public AliveMessage(SwimNode sourceNode) => (SourceNode, MessageType) = (sourceNode, MessageType.Alive);
    }
}
