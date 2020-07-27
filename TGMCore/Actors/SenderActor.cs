// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace TGMCore.Actors
{
    public class SenderActor : ReceiveActor
    {
        public SenderActor()
        {
            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            Receive<string>(str =>
            {
                var upperCase = str.ToUpper();
                mediator.Tell(new Send(path: "/user/destination", message: upperCase, localAffinity: true));
            });
        }
    }
}
