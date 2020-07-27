using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;

namespace TGMCore.Actors
{
    public class DestinationActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public DestinationActor()
        {
            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            mediator.Tell(new Put(Self));

            Receive<string>(s =>
            {
                _log.Info($"Got {s}");
            });
        }
    }
}
