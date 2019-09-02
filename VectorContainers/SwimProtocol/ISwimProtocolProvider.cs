using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SwimProtocol
{
    public interface ISwimProtocolProvider
    {
        event ReceivedMessageEventHandler ReceivedMessage;

        void OnMessageReceived(ReceivedMessageEventArgs e);

        SignedSwimMessage SignMessage(MessageBase message);

        void SendMessage(ISwimNode dest, CompositeMessage message);
        void SendMessage(ISwimNode dest, IEnumerable<SignedSwimMessage> messages);
        void SendMessage(ISwimNode dest, MessageBase message);

        ISwimNode Node { get; set; }
    }
}
