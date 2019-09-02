using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.API.LibSodium;
using Core.API.Membership;
using Core.API.Onion;
using Newtonsoft.Json;

namespace Core.API.POS
{
    public class LotteryService : ILotteryService
    {
        private IOnionServiceClient OnionServiceClient { get; }

        public LotteryService(IOnionServiceClient onionServiceClient)
        {
            OnionServiceClient = onionServiceClient;
        }

        public async Task<SignedLotteryTicket> GenerateSignedLotteryTicket(ulong round)
        {
            var ticket = LotteryTicket.Generate(round);
            var serialized = JsonConvert.SerializeObject(ticket);
            var hash = Cryptography.GenericHashNoKey(serialized);

            var signedResponse = await OnionServiceClient.SignHashAsync(hash);

            var signedTicket = new SignedLotteryTicket()
                {Hash = hash, PublicKey = signedResponse.PublicKey, Signature = signedResponse.Signature};

            return signedTicket;
        }
    }
}
