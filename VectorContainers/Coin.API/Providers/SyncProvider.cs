using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Core.API.Onion;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Coin.API.Providers
{
    public class SyncProvider
    {
        public bool IsRunning { get; private set; }

        private readonly IBlockGraphService blockGraphService;
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ITorClient torClient;
        private readonly ILogger logger;

        public SyncProvider(IBlockGraphService blockGraphService, IUnitOfWork unitOfWork, IHttpService httpService, ITorClient torClient, ILogger<SyncProvider> logger)
        {
            this.blockGraphService = blockGraphService;
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.torClient = torClient;
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

                var (local, network) = await Height();
                var numberOfBlocks = Difference(local, network);

                logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Local node block height ({local}). Network block height ({network}).");

                if (local < network)
                {
                    logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Synchronizing node. Total blocks behind ({numberOfBlocks})");

                    var downloads = await Synchronize(numberOfBlocks);
                    if (downloads.Any() != true)
                    {
                        blockGraphService.SetSynchronized(false);
                        logger.LogError($"<<< SyncProvider.SynchronizeCheck >>>: Synchronizing node failed. Number of blocks reached {local + (ulong)downloads.Count()} Expected Network block height ({network}");
                        return;
                    }

                    var sum = (ulong)downloads.Sum(v => v.Value);
                    if (!sum.Equals(numberOfBlocks))
                    {
                        blockGraphService.SetSynchronized(false);
                        return;
                    }

                    var total = local + sum;
                    unitOfWork.Interpreted.Store(total, total);
                }

                blockGraphService.SetSynchronized(true);
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
        public bool GetIsSynchronized()
        {
            return blockGraphService.IsSynchronized;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<ulong, int>>> Synchronize(ulong numberOfBlocks)
        {
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
                            var uri = new Uri(new Uri(httpService.RandomizedIP().Value), $"coins/{n * (long)numberOfBlocks}/{numberOfBlocks}");
                            var response = await torClient.GetAsync(uri, new CancellationToken());
                            var read = response.Content.ReadAsStringAsync().Result;
                            var jObject = JObject.Parse(read);
                            var jToken = jObject.GetValue("protobufs");
                            var byteArray = Convert.FromBase64String(jToken.Value<string>());
                            var blockIdProtos = Util.DeserializeListProto<BlockIDProto>(byteArray);

                            logger.LogInformation($"<<< Synchronize >>>: Retrieved {byteArray.Length} bytes from {uri.Host}");

                            var scheme = response.RequestMessage.RequestUri.Scheme;
                            var authority = response.RequestMessage.RequestUri.Authority;
                            var identity = httpService.Members.FirstOrDefault(k => k.Value.Equals($"{scheme}://{authority}"));

                            if (byteArray.Length > 0)
                            {
                                var blockIDs = blockIdProtos.Select(x => new Core.API.Consensus.BlockID(x.Hash, x.Node, x.Round, x.SignedBlock)).AsEnumerable();
                                var success = await blockGraphService.InterpretBlocks(blockIDs);

                                downloads.TryAdd(identity.Key, blockIDs.Count());
                                return;
                            }

                            downloads.TryAdd(identity.Key, 0);
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
                logger.LogError($"<<< SyncProvider.Synchronize >>>: Synchronize Node failed: {ex.ToString()}");
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
        private async Task<(ulong local, ulong network)> Height()
        {
            var l = (ulong)await blockGraphService.BlockHeight();
            var n = (ulong)await blockGraphService.NetworkBlockHeight();

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
