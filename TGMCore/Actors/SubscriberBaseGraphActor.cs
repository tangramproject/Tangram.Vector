// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using TGMCore.Model;
using TGMCore.Services;

namespace TGMCore.Actors
{
    public class SubscriberBaseGraphActor<TAttach> : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public SubscriberBaseGraphActor(IBlockGraphService<TAttach> blockGraphService, string topic)
        {
            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            mediator.Tell(new Subscribe(topic, Self));

            ReceiveAsync<byte[]>(async payload =>
            {
                try
                {
                    var blockGraphProtos = Helper.Util.DeserializeListProto<BaseGraphProto<TAttach>>(payload);
                    if (blockGraphProtos?.Any() == true)
                    {
                        for (int i = 0; i < blockGraphProtos.Count(); i++)
                        {
                            var added = await blockGraphService.SetBlockGraph(blockGraphProtos.ElementAt(i));
                            if (added != null)
                            {
                                _log.Error($"<<< SubscriberActor.ReceiveAsync >>>: " +
                                    $"Blockgraph: {blockGraphProtos.ElementAt(i).Block.Hash} was not add " +
                                    $"for node {blockGraphProtos.ElementAt(i).Block.Node} and round {blockGraphProtos.ElementAt(i).Block.Round}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"<<< SubscriberActor.ReceiveAsync >>>: {ex}");
                }
            });

            Receive<SubscribeAck>(subscribeAck =>
            {
                if (subscribeAck.Subscribe.Topic.Equals(topic)
                    && subscribeAck.Subscribe.Ref.Equals(Self)
                    && subscribeAck.Subscribe.Group == null)
                {
                    _log.Info("subscribing to SubscriberBaseGraphActor");
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphService"></param>
        /// <returns></returns>
        public static Props Create(IBlockGraphService<TAttach> blockGraphService, string topic) =>
            Props.Create(() => new SubscriberBaseGraphActor<TAttach>(blockGraphService, topic));
    }
}
