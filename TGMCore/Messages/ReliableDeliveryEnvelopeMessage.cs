// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0s

namespace TGMCore.Messages
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
