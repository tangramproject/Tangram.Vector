// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using TGMCore.Providers;
using TGMCore.Helper;
using TGMCore.Model;
using TGMCore.Messages;
using TGMCore.Extentions;

namespace TGMCore.Actors
{
    public class PublisherBaseGraphActor<TAttach> : ReceiveActor
    {
        private static readonly AsyncLock _markStatesAsMutex = new AsyncLock();
        private static readonly AsyncLock _markRepliesAsMutex = new AsyncLock();

        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly IUnitOfWork _unitOfWork;
        private readonly IClusterProvider _clusterProvider;
        private readonly IBaseGraphRepository<TAttach> _baseGraphRepository;
        private readonly IJobRepository<TAttach> _jobRepository;
        private readonly IActorRef _mediator;

        public PublisherBaseGraphActor(IUnitOfWork unitOfWork, IClusterProvider clusterProvider)
        {
            _unitOfWork = unitOfWork;
            _clusterProvider = clusterProvider;
            _jobRepository = unitOfWork.CreateJobOf<TAttach>();
            _baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            _mediator = DistributedPubSub.Get(Context.System).Mediator;

            Ready();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Ready()
        {
            ReceiveAsync<HashedMessage>(async message =>
            {
                if (_clusterProvider.AvailableMembersCount() == 0)
                {
                    return;
                }

                var blockGraphs = await _baseGraphRepository
                    .GetWhere(x => x.Block.Node == _clusterProvider.GetSelfUniqueAddress() && x.Block.Hash.Equals(message.Hash.ToHex()) && x.Included && !x.Replied);

                if (blockGraphs.Any() != true)
                {
                    return;
                }

                _mediator.Tell(new Publish(MessageType.BlockGraph.ToString(), Util.SerializeProto(blockGraphs)));

                var blockInfos = blockGraphs.Select(x => new BlockInfoProto { Hash = x.Block.Hash, Node = x.Block.Node, Round = x.Block.Round });

                await MarkMultipleStatesAs(blockInfos, JobState.Queued);
                await MarkMultipleRepliesAs(blockInfos, true);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockInfos"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task MarkMultipleStatesAs(IEnumerable<BlockInfoProto> blockInfos, JobState state)
        {
            if (blockInfos == null)
                throw new ArgumentNullException(nameof(blockInfos));

            try
            {
                using (await _markStatesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {   
                        return;
                    }

                    using var session = _unitOfWork.Document.OpenSession();

                    var filter = blockInfos.Select(async x => await _jobRepository.GetFirstOrDefault(g => g.Hash == x.Hash));
                    for (var i = filter.GetEnumerator(); i.MoveNext();)
                    {
                        switch (i.Current.Result.Status)
                        {
                            case JobState.Started:
                            case JobState.Queued:
                            case JobState.Answered:
                            case JobState.Dialling:
                                i.Current.Result.Status = state;
                                session.Store(i.Current.Result, null, i.Current.Result.Id);
                                break;
                            case JobState.Running:
                                break;
                            case JobState.Dead:
                                break;
                            case JobState.Pending:
                                break;
                            case JobState.Partial:
                                break;
                            case JobState.Blockmainia:
                                break;
                            case JobState.Polished:
                                break;
                            default:
                                break;
                        }
                    }

                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"<<< PublisherActor.MarkMultipleStatesAs >>>: {ex}");
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockInfos"></param>
        /// <param name="replied"></param>
        /// <returns></returns>
        private async Task MarkMultipleRepliesAs(IEnumerable<BlockInfoProto> blockInfos, bool replied = false)
        {
            if (blockInfos == null)
                throw new ArgumentNullException(nameof(blockInfos));

            try
            {
                using (await _markRepliesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {
                        return;
                    }

                    using var session = _unitOfWork.Document.OpenSession();

                    foreach (var next in blockInfos)
                    {
                        var blockGraph = await _baseGraphRepository
                            .GetFirstOrDefault(x =>
                            x.Block.Hash == next.Hash && x.Block.Node == _clusterProvider.GetSelfUniqueAddress() && x.Block.Round.Equals(next.Round));

                        if (blockGraph != null)
                        {
                            blockGraph.Replied = replied;
                            session.Store(blockGraph, null, blockGraph.Id);
                        }
                    }

                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _log.Error($"<<< PublisherActor.MarkMultipleRepliesAs >>>: {ex}");
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="clusterProvider"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IClusterProvider clusterProvider) =>
            Props.Create(() => new PublisherBaseGraphActor<TAttach>(unitOfWork, clusterProvider));
    }
}
