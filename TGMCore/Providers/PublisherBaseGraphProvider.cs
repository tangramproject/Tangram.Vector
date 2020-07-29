﻿// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Akka.Actor;
using Microsoft.Extensions.Logging;
using TGMCore.Actors;
using TGMCore.Messages;
using TGMCore.Model;

namespace TGMCore.Providers
{
    public class PublisherBaseGraphProvider<TAttach> : IPublisherBaseGraphProvider
    {
        private readonly IActorRef _actor;
        private readonly ILogger _logger;

        public PublisherBaseGraphProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork, IClusterProvider clusterProvider,
            IBaseGraphRepository<TAttach> baseGraphRepository, IJobRepository<TAttach> jobRepository,
            ILogger<PublisherBaseGraphProvider<TAttach>> logger)
        {
            _logger = logger;

            var publisher = PublisherBaseGraphActor<TAttach>.Create(unitOfWork, clusterProvider, baseGraphRepository, jobRepository);

            _actor = actorSystem.ActorOf(publisher, "publisher-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Publish(PublishMessage message)
        {
            _actor.Tell(message);
        }
    }
}
