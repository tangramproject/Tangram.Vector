using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.Model;
using Core.API.MQTT;
using Core.API.Network;
using Core.API.Services;
using Microsoft.Extensions.Logging;

namespace Core.API.Providers
{
    public class PubSubProvider<TAttach>
    {
        private static readonly AsyncLock markStatesAsMutex = new AsyncLock();
        private static readonly AsyncLock markRepliesAsMutex = new AsyncLock();
        private static readonly ScopedAsyncLock scopedAsyncLock = new ScopedAsyncLock();

        internal const string ReplicationBlockGraphTopic = "$internal/replica/blockgraph";
        internal const string BlockGraphTopic = "blockgraph";

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly IBlockGraphService<TAttach> blockGraphService;
        private readonly ILogger logger;
        private readonly IBaseGraphRepository<TAttach> baseGraphRepository;
        private readonly IJobRepository<TAttach> jobRepository;
        private readonly Publisher publisher;
        private readonly Subscriber subscriber;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public PubSubProvider(IUnitOfWork unitOfWork, IHttpClientService httpClientService, IBlockGraphService<TAttach> blockGraphSerivce,
            ILogger<PubSubProvider<TAttach>> logger, NodeEndPoint nodeEndPoint)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;
            this.blockGraphService = blockGraphSerivce;
            this.logger = logger;

            baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            jobRepository = unitOfWork.CreateJobOf<TAttach>();

            publisher = new Publisher(httpClientService.NodeIdentity, nodeEndPoint.Host, nodeEndPoint.Port);
            subscriber = new Subscriber(httpClientService.NodeIdentity, nodeEndPoint.Host, nodeEndPoint.Port, BlockGraphTopic);

            subscriber.MqttApplicationMessageReceived += Subscriber_MqttApplicationMessageReceived;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            try
            {
                await subscriber.Start();
                await publisher.Start();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< PubSubProvider.Start >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Publish()
        {
            SemaphoreSlim throttler = null;

            try
            {
                var blockGraphs = await baseGraphRepository
                    .GetWhere(x => x.Block.Node.Equals(httpClientService.NodeIdentity) && x.Included && !x.Replied);

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
                            var currentBlockGraphs = Util.SerializeProto(batch);
                            var result = await publisher.Publish(ReplicationBlockGraphTopic, currentBlockGraphs);
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
                logger.LogError($"<<< PubSubProvider.Publish >>>: {ex.ToString()}");
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
                using (await markStatesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {
                        return;
                    }

                    using var session = unitOfWork.Document.OpenSession();

                    var filter = blockInfos.Select(async x => await jobRepository.GetFirstOrDefault(g => g.Hash.Equals(x.Hash)));
                    for (var i = filter.GetEnumerator(); i.MoveNext();)
                    {
                        var x = i.Current;
                        if (!x.Status.Equals(JobState.Started) &&
                            !x.Status.Equals(JobState.Queued) &&
                            !x.Status.Equals(JobState.Answered) &&
                            !x.Status.Equals(JobState.Dialling))
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
                logger.LogError($"<<< PubSubProvider.MarkMultipleStatesAs >>>: {ex.ToString()}");
            }

            return;
        }

        private async Task MarkMultipleRepliesAs(IEnumerable<BlockInfoProto> blockInfos, bool replied = false)
        {
            if (blockInfos == null)
                throw new ArgumentNullException(nameof(blockInfos));

            try
            {
                using (await markRepliesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {
                        return;
                    }

                    using var session = unitOfWork.Document.OpenSession();

                    foreach (var next in blockInfos)
                    {
                        var blockGraph = await baseGraphRepository
                            .GetFirstOrDefault(x => x.Block.Hash.Equals(next.Hash) && x.Block.Node.Equals(httpClientService.NodeIdentity) && x.Block.Round.Equals(next.Round));
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
                logger.LogError($"<<< PubSubProvider.MarkMultipleRepliesAs >>>: {ex.ToString()}");
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Subscriber_MqttApplicationMessageReceived(object sender, MQTTnet.MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                using var scopedLock = await scopedAsyncLock.CreateLockAsync();

                var blockGraphProtos = Util.DeserializeListProto<BaseGraphProto<TAttach>>(args.ApplicationMessage.Payload);
                if (blockGraphProtos?.Any() == true)
                {
                    for (int i = 0; i < blockGraphProtos.Count(); i++)
                    {
                        var added = await blockGraphService.SetBlockGraph(blockGraphProtos.ElementAt(i));
                        if (added != null)
                        {
                            logger.LogError($"<<< PubSubProvider.Subscriber_MqttApplicationMessageReceived >>>: " +
                                $"Blockgraph: {blockGraphProtos.ElementAt(i).Block.Hash} was not add " +
                                $"for node {blockGraphProtos.ElementAt(i).Block.Node} and round {blockGraphProtos.ElementAt(i).Block.Round}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< PubSubProvider.Subscriber_MqttApplicationMessageReceived >>>: {ex.ToString()}");
            }
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (cancellationTokenSource != null)
                    {
                        cancellationTokenSource.Cancel();
                        cancellationTokenSource.Dispose();
                        cancellationTokenSource = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
