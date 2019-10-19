using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly NetworkProvider networkProvider;
        private readonly InterpretBlocksProvider interpretBlocksProvider;
        private readonly ILogger logger;

        public SyncProvider(IUnitOfWork unitOfWork, IHttpService httpService, NetworkProvider networkProvider,
            InterpretBlocksProvider interpretBlocksProvider, ILogger<SyncProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.networkProvider = networkProvider;
            this.interpretBlocksProvider = interpretBlocksProvider;
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
                var maxNetworkHeight = (ulong)network.Max(m => m.BlockCount);
                var numberOfBlocks = Difference(local, maxNetworkHeight);
                var maxNetworks = network.Where(x => x.BlockCount == (long)maxNetworkHeight);

                logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Local node block height ({local}). Network block height ({maxNetworks}).");

                if (local < maxNetworkHeight)
                {
                    logger.LogInformation($"<<< SyncProvider.SynchronizeCheck >>>: Synchronizing node. Total blocks behind ({numberOfBlocks})");

                    var downloads = await Synchronize(maxNetworks, numberOfBlocks);
                    if (downloads.Any() != true)
                    {
                        IsSynchronized = false;
                        logger.LogError($"<<< SyncProvider.SynchronizeCheck >>>: Failed to synchronize node. Number of blocks reached {local + (ulong)downloads.Count()} Expected Network block height ({maxNetworks}");
                        return;
                    }

                    var sum = (ulong)downloads.Sum(v => v.Value);
                    if (!sum.Equals(numberOfBlocks))
                    {
                        IsSynchronized = false;
                        return;
                    }

                    await SetInterpreted(maxNetworks.ToArray());
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
                            var read = response.Content.ReadAsStringAsync().Result;
                            var jObject = JObject.Parse(read);
                            var jToken = jObject.GetValue("protobufs");
                            var byteArray = Convert.FromBase64String(jToken.Value<string>());
                            var blockIdProtos = Util.DeserializeListProto<BlockIDProto>(byteArray);

                            logger.LogInformation($"<<< Synchronize >>>: Retrieved {byteArray.Length} bytes from {response.RequestMessage.RequestUri.Authority}");

                            var fullIdentity = httpService.GetFullNodeIdentity(response);

                            if (byteArray.Length > 0)
                            {
                                var blockIDs = blockIdProtos.Select(x => new Core.API.Consensus.BlockID(x.Hash, x.Node, x.Round, x.SignedBlock)).AsEnumerable();
                                var success = await interpretBlocksProvider.Interpret(blockIDs);

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
        /// <param name="blockCountProtos"></param>
        /// <returns></returns>
        private async Task SetInterpreted(NodeBlockCountProto[] blockCountProtos)
        {
            try
            {
                var list = new List<InterpretedProto>();

                Util.Shuffle(blockCountProtos);

                var addresses = blockCountProtos.Select(x => x.Address);
                var responses = await httpService.Dial(DialType.Get, addresses, "interpreted");

                foreach (var response in responses)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var read = response.Content.ReadAsStringAsync().Result;
                        var jObject = JObject.Parse(read);
                        var jToken = jObject.GetValue("interpreted");
                        var byteArray = Convert.FromBase64String(jToken.Value<string>());

                        if (byteArray.Length > 0)
                        {
                            var interpretedProto = Util.DeserializeProto<InterpretedProto>(byteArray);
                            list.Add(interpretedProto);
                        }
                    }
                }

                var interpreted = list.Where(x => x.Round == list.Max(m => m.Round));

                Util.Shuffle(interpreted.ToArray());

                unitOfWork.Interpreted.Store(interpreted.First().Consumed, interpreted.First().Round);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SyncProvider.SetInterpreted >>>: Failed to set interpreted: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<(ulong local, IEnumerable<NodeBlockCountProto> network)> Height()
        {
            var l = (ulong)await networkProvider.BlockHeight();
            var n = await networkProvider.FullNetworkBlockHeight();

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
