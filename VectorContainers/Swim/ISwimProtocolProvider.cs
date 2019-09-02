using Swim.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim
{
    public interface ISwimProtocolProvider
    {
        event ReceivedMessageEventHandler ReceivedMessage;
        void OnMessageReceived(ReceivedMessageEventArgs e);
        void SendMessage(SwimNode dest, MessageBase message);
        SwimNode Node { get; set; }
    }
}
