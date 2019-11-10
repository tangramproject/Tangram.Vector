using System;
namespace Core.API.Messages
{
    public class ReliableDeliveryEnvelopeMessage<TMessage>
    {
        public ReliableDeliveryEnvelopeMessage(TMessage message, long messageId)
        {
            Message = message;
            MessageId = messageId;
        }

        public TMessage Message { get; private set; }

        public long MessageId { get; private set; }
    }
}
