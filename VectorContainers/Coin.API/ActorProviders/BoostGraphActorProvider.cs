using System;
using System.Threading.Tasks;
using Akka.Actor;
using Coin.API.Actors;
using Coin.API.Services;
using Core.API.Messages;
using Core.API.Model;

namespace Coin.API.ActorProviders
{
    public class BoostGraphActorProvider: IBoostGraphActorProvider
    {
        private readonly IActorRef actor;

        public BoostGraphActorProvider(ActorSystem actotSystem, IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
            IProcessBlockActorProvider processBlockActorProvider,ISigningActorProvider signingActorProvider)
        {
            actor = actotSystem.ActorOf(Props.Create(() => new BoostGraphActor
                (
                    unitOfWork,
                    httpService,
                    interpretActorProvider,
                    processBlockActorProvider,
                    signingActorProvider
                )), "boostgraph-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Process(ProcessBlockMessage message)
        {
            actor.Tell(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task RegisterAsync(HashedMessage message)
        {
            actor.Tell(message);
            return Task.CompletedTask;
        }
    }
}
