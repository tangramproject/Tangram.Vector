// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using TGMCore.Messages;
using TGMCore.Model;
using TGMCore.Services;

namespace TGMCore.Actors
{
    public class Cancel { };
    public class Finished { };
    public class Failed { };

    public class SubscriberBaseGraphActor<TAttach> : ReceiveActor, IWithUnboundedStash
    {
        private readonly IBlockGraphService<TAttach> _blockGraphService;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private CancellationTokenSource _cancel;

        public IStash Stash { get; set; }

        public SubscriberBaseGraphActor(IBlockGraphService<TAttach> blockGraphService)
        {
            _cancel = new CancellationTokenSource();
            _blockGraphService = blockGraphService;

            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            mediator.Tell(new Subscribe(MessageType.BlockGraph.ToString(), Self));

            Ready();

            Ack();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Ready()
        {
            Receive<byte[]>(payload =>
            {
                var self = Self;

                Task.Run(() =>
                {
                    IEnumerable<BaseGraphProto<TAttach>> deserBlocks = null;
                    try
                    {
                        deserBlocks = Helper.Util.DeserializeListProto<BaseGraphProto<TAttach>>(payload);
                    }
                    catch (Exception)
                    {
                        _log.Error($"<<< SubscriberBaseGraphActor.Receive >>>: Could not deserialize payload");
                    }
                    return deserBlocks;
                }, _cancel.Token).ContinueWith(async blocks =>
                {
                    if (blocks.IsCanceled || blocks.IsFaulted)
                    {
                        self.Tell(new Cancel());
                        return;
                    }

                    if (blocks.Result?.Any() == true)
                    {
                        for (int i = 0; i < blocks.Result.Count(); i++)
                        {
                            var added = await _blockGraphService.SetBlockGraph(blocks.Result.ElementAt(i));
                            if (added == null)
                            {
                                _log.Error($"<<< SubscriberBaseGraphActor.Receive >>>: " +
                                    $"Blockgraph: {blocks.Result.ElementAt(i).Block.Hash} was not add " +
                                    $"for node {blocks.Result.ElementAt(i).Block.Node} and round {blocks.Result.ElementAt(i).Block.Round}");
                            }
                        }
                    }

                    self.Tell(new Finished());

                }, TaskContinuationOptions.ExecuteSynchronously)
                .PipeTo(self);

                Become(Working);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        private void Ack()
        {
            Receive<SubscribeAck>(subscribeAck =>
            {
                if (subscribeAck.Subscribe.Topic.Equals(MessageType.BlockGraph.ToString())
                    && subscribeAck.Subscribe.Ref.Equals(Self)
                    && subscribeAck.Subscribe.Group == null)
                {
                    _log.Info($"subscribing to {subscribeAck.Subscribe.Topic}");
                }
            });
        }

        private void Working()
        {
            Receive<Cancel>(cancel =>
            {
                _cancel.Cancel(); // cancel work
                BecomeReady();
            });
            Receive<Failed>(f => BecomeReady());
            Receive<Finished>(f => BecomeReady());
            ReceiveAny(o => Stash.Stash());
        }

        private void BecomeReady()
        {
            _cancel = new CancellationTokenSource();
            Stash.UnstashAll();
            Become(Ready);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphService"></param>
        /// <returns></returns>
        public static Props Create(IBlockGraphService<TAttach> blockGraphService) => Props.Create(() => new SubscriberBaseGraphActor<TAttach>(blockGraphService));
    }
}
