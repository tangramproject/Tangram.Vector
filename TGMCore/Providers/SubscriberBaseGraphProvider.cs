// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Akka.Actor;
using Microsoft.Extensions.Logging;
using TGMCore.Actors;
using TGMCore.Services;

namespace TGMCore.Providers
{
    public class SubscriberBaseGraphProvider<TAttach> : ISubscriberBaseGraphProvider
    {
        private readonly IActorRef _actor;
        private readonly ILogger _logger;

        public SubscriberBaseGraphProvider(IActorSystemService actorSystemService, IBlockGraphService<TAttach> blockGraphService,
            ILogger<SubscriberBaseGraphProvider<TAttach>> logger)
        {
            _logger = logger;

            var subscriber = SubscriberBaseGraphActor<TAttach>.Create(blockGraphService);

            _actor = actorSystemService.Get.ActorOf(subscriber, "subscriber-actor");
        }
    }
}
