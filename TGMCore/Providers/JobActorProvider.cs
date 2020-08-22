// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Akka.Actor;
using TGMCore.Actors;
using TGMCore.Messages;
using TGMCore.Model;
using TGMCore.Services;

namespace TGMCore.Providers
{
    public class JobActorProvider<TAttach>: IJobActorProvider<TAttach>
    {
        private readonly IActorRef _actor;

        public JobActorProvider(IActorSystemService actorSystemService, IUnitOfWork unitOfWork, IClusterProvider clusterProvider, IPublisherBaseGraphProvider publisherBaseGraphProvider)
        {
            var jobActorProps = JobActor<TAttach>.Create(unitOfWork, clusterProvider, publisherBaseGraphProvider);
            _actor = actorSystemService.Get.ActorOf(jobActorProps, "job-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Register(HashedMessage message)
        {
            _actor.Tell(message);
        }
    }
}
