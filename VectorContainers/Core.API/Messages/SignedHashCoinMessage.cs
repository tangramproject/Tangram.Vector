using Core.API.Model;

namespace Core.API.Messages
{
    public class SignedHashCoinMessage
    {
        public CoinProto Coin { get; }
        public byte[] Key { get; }

        public SignedHashCoinMessage(CoinProto coin, byte[] key = null)
        {
            Coin = coin;
            Key = key;
        }
    }
}
