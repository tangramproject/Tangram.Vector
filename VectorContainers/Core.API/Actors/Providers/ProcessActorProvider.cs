using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Core.API.Messages;
using Microsoft.Extensions.Logging;

namespace Core.API.Actors.Providers
{
    public class ProcessActorProvider<TAttach> : IProcessActorProvider<TAttach>
    {
        private readonly IActorRef actor;

        public ProcessActorProvider(ActorSystem actotSystem, ISigningActorProvider signingActorProvider, ILogger<ProcessActorProvider<TAttach>> logger)
        {
            var actorProps = ProcessActor<TAttach>.Create(signingActorProvider).WithRouter(new RoundRobinPool(5));
            actor = actotSystem.ActorOf(actorProps, "process-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> Process(BlockGraphMessage<TAttach> message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
