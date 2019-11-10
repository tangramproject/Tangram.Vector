using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Coin.API.Actors;
using Core.API.Messages;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Coin.API.ActorProviders
{
    public interface IProcessBlockActorProvider
    {
        Task<BlockGraphProto> ProcessBlock(BlockGraphMessage message);
    }

    public class ProcessBlockActorProvider : IProcessBlockActorProvider
    {
        private readonly IActorRef actor;

        public ProcessBlockActorProvider(ActorSystem actotSystem, ISigningActorProvider signingActorProvider, ILogger<ProcessBlockActorProvider> logger)
        {
            var actorProps = ProcessBlockActor.Props(signingActorProvider).WithRouter(new RoundRobinPool(5));
            actor = actotSystem.ActorOf(actorProps, "process-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> ProcessBlock(BlockGraphMessage message)
        {
            return await actor.Ask<BlockGraphProto>(message);
        }
    }
}
