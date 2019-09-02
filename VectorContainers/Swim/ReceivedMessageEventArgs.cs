using Swim.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim
{
    public class ReceivedMessageEventArgs : EventArgs
    {
        public SwimNode Source { get; set; }
        public MessageBase Message { get; set; }
    }
}
