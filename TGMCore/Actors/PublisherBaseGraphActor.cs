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
using TGMCore.Providers;
using TGMCore.Helper;
using TGMCore.Model;

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

        public PublisherBaseGraphActor(IUnitOfWork unitOfWork, IClusterProvider clusterProvider,
            IBaseGraphRepository<TAttach> baseGraphRepository, IJobRepository<TAttach> jobRepository)
        {
            _unitOfWork = unitOfWork;
            _clusterProvider = clusterProvider;
            _baseGraphRepository = baseGraphRepository;
            _jobRepository = jobRepository;

            _mediator = DistributedPubSub.Get(Context.System).Mediator;

            ReceiveAsync<string>(async topic => await Publish(topic));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        private async Task Publish(string topic)
        {
            SemaphoreSlim throttler = null;

            try
            {
                var blockGraphs = await _baseGraphRepository
                    .GetWhere(x => x.Block.Node == _clusterProvider.GetSelfUniqueAddress() && x.Included && !x.Replied);

                if (blockGraphs.Any() != true)
                {
                    return;
                }

                var tasks = new List<Task>();
                var numberOfBlocks = 100;
                var numberOfBatches = (int)Math.Ceiling((double)numberOfBlocks / numberOfBlocks);

                throttler = new SemaphoreSlim(int.MaxValue);

                var series = new long[numberOfBatches];
                foreach (var n in series)
                {
                    var batch = blockGraphs.Skip((int)(n * numberOfBlocks)).Take(numberOfBlocks);
                    if (batch.Any() != true)
                    {
                        break;
                    }

                    await throttler.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            _mediator.Tell(new Publish(topic, Util.SerializeProto(batch)));

                            var blockInfos = batch.Select(x => new BlockInfoProto { Hash = x.Block.Hash, Node = x.Block.Node, Round = x.Block.Round });

                            await MarkMultipleStatesAs(blockInfos, JobState.Queued);
                            await MarkMultipleRepliesAs(blockInfos, true);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                _log.Error($"<<< PublisherActor.Publish >>>: {ex}");
            }
            finally
            {
                throttler?.Dispose();
            }
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
                        var x = i.Current;
                        if (x.Result.Status != JobState.Started &&
                            x.Result.Status != JobState.Queued &&
                            x.Result.Status != JobState.Answered &&
                            x.Result.Status != JobState.Dialling)
                        {
                            continue;
                        }

                        x.Result.Status = state;
                        session.Store(x, null, x.Result.Id);
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
                            .GetFirstOrDefault(x => x.Block.Hash == next.Hash && x.Block.Node == _clusterProvider.GetSelfUniqueAddress() && x.Block.Round.Equals(next.Round));
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
        /// <param name="baseGraphRepository"></param>
        /// <param name="jobRepository"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IClusterProvider clusterProvider,
            IBaseGraphRepository<TAttach> baseGraphRepository, IJobRepository<TAttach> jobRepository) =>
            Props.Create(() => new PublisherBaseGraphActor<TAttach>(unitOfWork, clusterProvider, baseGraphRepository, jobRepository));
    }
}
