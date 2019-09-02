using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public class ReceivedMessageEventArgs : EventArgs
    {
        public CompositeMessage CompositeMessage { get; set; }
    }
}
