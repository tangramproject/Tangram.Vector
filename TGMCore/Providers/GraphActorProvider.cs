// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using Akka.Actor;
using TGMCore.Actors;
using TGMCore.Messages;
using TGMCore.Model;
using TGMCore.Services;

namespace TGMCore.Providers
{
    public class GraphActorProvider<TAttach> : IGraphActorProvider<TAttach>
    {
        private readonly IActorRef _actor;

        public GraphActorProvider(IActorSystemService actorSystemService, IUnitOfWork unitOfWork,
            IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider,
            IJobActorProvider<TAttach> jobActorProvider)
        {

            var graphActorProps = GraphActor<TAttach>.Create(unitOfWork, clusterProvider, interpretActorProvider, processActorProvider, signingActorProvider, jobActorProvider);
            _actor = actorSystemService.Get.ActorOf(graphActorProps, "graph-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task Process(ProcessBlockMessage<TAttach> message)
        {
            _actor.Tell(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task RegisterAsync(HashedMessage message)
        {
            _actor.Tell(message);
            return Task.CompletedTask;
        }
    }
}
