using Core.API.Model;

namespace Core.API.Messages
{
    public class ValidateCoinRuleMessage
    {
        public CoinProto Coin { get; }

        public ValidateCoinRuleMessage(CoinProto coin)
        {
            Coin = coin;
        }
    }
}
