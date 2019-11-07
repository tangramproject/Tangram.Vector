using Core.API.Model;

namespace Core.API.Messages
{
    public class VerifiyHashChainMessage
    {
        public CoinProto Previous { get; }
        public CoinProto Next { get; }

        public VerifiyHashChainMessage(CoinProto previous, CoinProto next)
        {
            Previous = previous;
            Next = next;
        }
    }
}
