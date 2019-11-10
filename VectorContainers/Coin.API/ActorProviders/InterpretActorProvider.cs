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
    public class InterpretActorProvider : IInterpretActorProvider
    {
        private readonly IActorRef actor;

        public InterpretActorProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider, ILogger<InterpretActorProvider> logger)
        {
            var actorProps = InterpretActor.Props(unitOfWork, signingActorProvider).WithRouter(new RoundRobinPool(5));
            actor = actorSystem.ActorOf(actorProps, "interpret-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> Interpret(InterpretBlocksMessage message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
