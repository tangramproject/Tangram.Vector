using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.ActorProviders;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Coin.API.Providers
{
    public class SyncProvider
    {
        public bool IsRunning { get; private set; }
        public bool IsSynchronized { get; private set; }

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly INetworkActorProvider networkActorProvider;
        private readonly IInterpretActorProvider interpretActorProvider;
        private readonly ILogger logger;

        public SyncProvider(IUnitOfWork unitOfWork, IHttpService httpService, INetworkActorProvider networkActorProvider,
            IInterpretActorProvider interpretActorProvider, ILogger<SyncProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.networkActorProvider = networkActorProvider;
            this.interpretActorProvider = interpretActorProvider;
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
                            Util.Shuffle(pool.ToArray());

                            var response = await httpService.Dial(DialType.Get, pool.First().Address, $"coins/{n * (long)numberOfBlocks}/{numberOfBlocks}");

                            var read = Util.ReadJToken(response, "protobufs");
                            var byteArray = Convert.FromBase64String(read.Value<string>());
                            var blockIdProtos = Util.DeserializeListProto<BlockIDProto>(byteArray);

                            logger.LogInformation($"<<< Synchronize >>>: Retrieved {byteArray.Length} bytes from {response.RequestMessage.RequestUri.Authority}");

                            var fullIdentity = httpService.GetFullNodeIdentity(response);

                            if (byteArray.Length > 0)
                            {
                                var blockIDs = blockIdProtos.Select(x => new Core.API.Consensus.BlockID(x.Hash, x.Node, x.Round, x.SignedBlock)).AsEnumerable();
                                var success = await interpretActorProvider.Interpret(new Core.API.Messages.InterpretBlocksMessage(httpService.NodeIdentity, blockIDs));

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
