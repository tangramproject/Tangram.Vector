using System;
using System.Collections.Generic;
using System.Text;
using Swim.Messages;

namespace Swim.Tests
{
    class SwimProtocolProvider : ISwimProtocolProvider
    {
        public SwimNode Node { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event ReceivedMessageEventHandler ReceivedMessage;

        public SwimNode GetThisNode()
        {
            return new SwimNode("localhost");
        }

        public void OnMessageReceived(ReceivedMessageEventArgs e)
        {
            ReceivedMessage(this, new ReceivedMessageEventArgs { Message = e.Message, Source = e.Source });
        }

        public void SendMessage(SwimNode dest, MessageBase message)
        {

        }
    }
}
