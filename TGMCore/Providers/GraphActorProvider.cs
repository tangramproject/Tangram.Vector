// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using Akka.Actor;
using TGMCore.Actors;
using TGMCore.Messages;
using TGMCore.Model;

namespace TGMCore.Providers
{
    public class GraphActorProvider<TAttach> : IGraphActorProvider<TAttach>
    {
        private readonly IActorRef actor;

        public GraphActorProvider(ActorSystem actotSystem, IUnitOfWork unitOfWork,
            IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider,ISigningActorProvider signingActorProvider)
        {
            actor = actotSystem.ActorOf(Props.Create(() => new GraphActor<TAttach>
                (
                    unitOfWork,
                    clusterProvider,
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
