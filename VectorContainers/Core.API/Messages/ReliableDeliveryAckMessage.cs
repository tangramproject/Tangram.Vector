using System;
namespace Core.API.Messages
{
    public class ReliableDeliveryAckMessage
    {
        public ReliableDeliveryAckMessage(long messageId)
        {
            MessageId = messageId;
        }

        public long MessageId { get; private set; }
    }
}
