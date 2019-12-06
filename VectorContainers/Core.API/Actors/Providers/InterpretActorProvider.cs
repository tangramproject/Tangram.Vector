using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Core.API.Messages;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Core.API.Actors.Providers
{
    public class InterpretActorProvider<TModel> : IInterpretActorProvider<TModel>
    {
        private readonly IActorRef actor;

        public InterpretActorProvider(ActorSystem actorSystem, Func<IUnitOfWork, ISigningActorProvider, Props> invoker,
            IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider, ILogger<InterpretActorProvider<TModel>> logger)
        {
            var actorProps = invoker(unitOfWork, signingActorProvider).WithRouter(new RoundRobinPool(5));
            actor = actorSystem.ActorOf(actorProps, "interpret-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> Interpret(InterpretMessage<TModel> message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
