using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Coin.API.Actors;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Models;
using Core.API.Onion;
using Microsoft.Extensions.Logging;

namespace Coin.API.ActorProviders
{
    public class SigningActorProvider : ISigningActorProvider
    {
        private readonly IActorRef actor;

        public SigningActorProvider(ActorSystem actorSystem, IOnionServiceClient onionServiceClient, ILogger<SigningActor> logger)
        {
            var actorProps = SigningActor.Props(onionServiceClient).WithRouter(new RoundRobinPool(5));
            actor = actorSystem.ActorOf(actorProps, "signing-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<byte[]> BlockHash(SignedBlockHashMessage message)
        {
            return await actor.Ask<byte[]>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<byte[]> HashCoin(SignedHashCoinMessage message)
        {
            return await actor.Ask<byte[]>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> Sign(SignedBlockGraphMessage message)
        {
            return await actor.Ask<BlockGraphProto>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<SignedHashResponse> Sign(SignedHashMessage message)
        {
            return await actor.Ask<SignedHashResponse>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> ValidateCoinRule(ValidateCoinRuleMessage message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifiyBlockSignature(VerifiyBlockSignatureMessage message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifiyHashChain(VerifiyHashChainMessage message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifiySignature(VerifiySignatureMessage message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
