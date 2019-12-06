using System.Threading.Tasks;
using Akka.Actor;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;

namespace Core.API.Actors.Providers
{
    public class GraphActorProvider<TAttach> : IGraphActorProvider<TAttach>
    {
        private readonly IActorRef actor;

        public GraphActorProvider(ActorSystem actotSystem, IUnitOfWork unitOfWork, IHttpClientService httpClientService, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider)
        {
            actor = actotSystem.ActorOf(Props.Create(() => new GraphActor<TAttach>
                (
                    unitOfWork,
                    httpClientService,
                    interpretActorProvider,  
                    processActorProvider,
                    signingActorProvider
                )), "graph-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Process(ProcessBlockMessage<TAttach> message)
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
