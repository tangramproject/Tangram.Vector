using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Actors.Providers;
using Core.API.Model;
using Core.API.Network;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Core.API.Providers
{
    public class SyncProvider<TAttach>
    {
        public bool IsRunning { get; private set; }
        public bool IsSynchronized { get; private set; }
        public string Route { get; }

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly INetworkActorProvider networkActorProvider;
        private readonly IInterpretActorProvider<TAttach> interpretActorProvider;
        private readonly ILogger logger;

        public SyncProvider(IUnitOfWork unitOfWork, IHttpClientService httpClientService, INetworkActorProvider networkActorProvider,
            IInterpretActorProvider<TAttach> interpretActorProvider, string route, ILogger<SyncProvider<TAttach>> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;
            this.networkActorProvider = networkActorProvider;
            this.interpretActorProvider = interpretActorProvider;

            Route = route;

            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SynchronizeCheck()
        {
            IsRunning = true;

            try
            {
                logger.LogInformation("<<< SyncProvider.SynchronizeCheck >>>: Checking block height.");

                var maxNetworks = Enumerable.Empty<NodeBlockCountProto>();
                ulong maxNetworkHeight = 0;

                var (local, network) = await Height();

                if (network.Any())
                {
                    maxNetworkHeight = network.Max(m => m.BlockCount);
                    maxNetworks = network.Where(x => x.BlockCount == maxNetworkHeight);
                }

                logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Local node block height ({local}). Network block height ({maxNetworkHeight}).");

                if (local < maxNetworkHeight)
                {
                    var numberOfBlocks = Difference(local, maxNetworkHeight);

                    logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Synchronizing node. Total blocks behind ({numberOfBlocks})");

                    var downloads = await Synchronize(maxNetworks, numberOfBlocks);
                    if (downloads.Any() != true)
                    {
                        IsSynchronized = false;
                        logger.LogError($"<<< SyncProvider.SynchronizeCheck >>>: Failed to synchronize node. Number of blocks reached {local + (ulong)downloads.Count()} Expected Network block height ({maxNetworkHeight}");
                        return;
                    }

                    var downloadSum = (ulong)downloads.Sum(v => v.Value);
                    if (!downloadSum.Equals(numberOfBlocks))
                    {
                        IsSynchronized = false;
                        return;
                    }
                }

                IsSynchronized = true;
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SyncProvider.SynchronizeCheck >>>: {ex.ToString()}");
            }

            IsRunning = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<ulong, int>>> Synchronize(IEnumerable<NodeBlockCountProto> pool, ulong numberOfBlocks)
        {
            if (pool.Any() != true)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            var throttler = new SemaphoreSlim(int.MaxValue);
            var downloads = new ConcurrentDictionary<ulong, int>();

            try
            {
                var allTasks = new List<Task>();
                var numberOfBatches = (int)Math.Ceiling((double)numberOfBlocks / numberOfBlocks);

                var series = new long[numberOfBatches];
                foreach (var n in series)
                {
                    await throttler.WaitAsync();

                    allTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            Helper.Util.Shuffle(pool.ToArray());

                            var response = await httpClientService.Dial(DialType.Get, pool.First().Address, $"{Route}/{n * (long)numberOfBlocks}/{numberOfBlocks}");

                            var read = Helper.Util.ReadJToken(response, "protobufs");
                            var byteArray = Convert.FromBase64String(read.Value<string>());
                            var blockIdProtos = Helper.Util.DeserializeListProto<BaseBlockIDProto<TAttach>>(byteArray);

                            logger.LogInformation($"<<< Synchronize >>>: Retrieved {byteArray.Length} bytes from {response.RequestMessage.RequestUri.Authority}");

                            var fullIdentity = httpClientService.GetFullNodeIdentity(response);

                            if (byteArray.Length > 0)
                            {
                                var blockIDs = blockIdProtos.Select(x => new BaseBlockIDProto<TAttach>
                                {
                                    Hash = x.Hash,
                                    Node = x.Node,
                                    Round = x.Round,
                                    SignedBlock = x.SignedBlock
                                }).AsEnumerable();

                                var success = await interpretActorProvider.Interpret(new Messages.InterpretMessage<TAttach>(httpClientService.NodeIdentity, blockIDs));

                                downloads.TryAdd(fullIdentity.Key, blockIDs.Count());
                                return;
                            }

                            downloads.TryAdd(fullIdentity.Key, 0);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }

                try
                {
                    await Task.WhenAll(allTasks);
                }
                catch { }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SyncProvider.Synchronize >>>: Failed to synchronize node: {ex.ToString()}");
            }
            finally
            {
                throttler.Dispose();
            }

            return downloads;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<(ulong local, IEnumerable<NodeBlockCountProto> network)> Height()
        {
            var l = (ulong)await networkActorProvider.BlockHeight();
            var n = await networkActorProvider.FullNetworkBlockHeight();

            return (l, n);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="local"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private ulong Difference(ulong local, ulong network)
        {
            return network > local ? network - local : local - network;
        }
    }
}
