using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Model;
using Core.API.Onion;
using Core.API.Signatures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Secp256k1_ZKP.Net;

namespace Coin.API.Services
{
    public class BlockGraphService : IBlockGraphService
    {
        public bool IsSynchronized { get; private set; }

        private const int processBlocksInterval = 1 * 60 * 32;
        private const int requiredNodeCount = 4;

        private static readonly AsyncLock processBlocksMutex = new AsyncLock();
        private static readonly AsyncLock interpretBlocksMutex = new AsyncLock();
        private static readonly AsyncLock addBlockGraphMutex = new AsyncLock();

        private Graph Graph;
        private Config Config;

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ITorClient torClient;
        private readonly ILogger logger;

        private readonly HierarchicalDataProvider dataProvider;
        private readonly SigningProvider signingProvider;

        private Timer processBlocksTimer;
        private ulong lastInterpreted;
        private ulong round;

        public BlockGraphService(IUnitOfWork unitOfWork, IHttpService httpService, HierarchicalDataProvider dataProvider, SigningProvider signingProvider,
            ITorClient torClient, ILogger<BlockGraphService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.dataProvider = dataProvider;
            this.signingProvider = signingProvider;
            this.torClient = torClient;
            this.logger = logger;

            Start().GetAwaiter();
        }

        /// <summary>
        /// 
        /// </summary>
        private async Task Start()
        {
            logger.LogInformation("<<< Initialize >>>: Starting Block Graph Service.");

            var totalNodes = httpService.Members.Count + 1;
            if (totalNodes < requiredNodeCount)
            {
                logger.LogWarning($"<<< Initialize >>>: Minimum number of nodes required (4). Total number of nodes ({totalNodes})");
            }

            while (!IsSynchronized)
            {
                await Task.Delay(1000);
            }

            lastInterpreted = await unitOfWork.Interpreted.GetRound();
            lastInterpreted = lastInterpreted > 0 ? lastInterpreted - 1 : lastInterpreted;

            round = lastInterpreted;

            Config = new Config(lastInterpreted, new ulong[totalNodes], httpService.NodeIdentity, (ulong)totalNodes);
            Graph = new Graph(Config, BlockmaniaCallback);

            processBlocksTimer = new Timer((state) => ProcessBlocks().SwallowException(), null, processBlocksInterval, System.Threading.Timeout.Infinite);

            logger.LogInformation("<<< Initialize >>>: Started Block Graph Service.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="synced"></param>
        public void SetSynchronized(bool synced)
        {
            IsSynchronized = synced;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> AddBlockGraph(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            using (await addBlockGraphMutex.LockAsync())
            {
                try
                {
                    if (blockGraph.Block.Node.Equals(httpService.NodeIdentity))
                    {
                        var self = await AddToSelf(blockGraph);
                        if (self == null)
                        {
                            logger.LogError($"<<< AddBlockGraph >>>: Could not add self graph to mem pool for block {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                            return null;
                        }

                        return self;
                    }

                    var isSelf = await AddToSelf(blockGraph);
                    if (isSelf == null)
                    {
                        logger.LogError($"<<< AddBlockGraph >>>: Exists or could not add self graph to mem pool for block {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                        return null;
                    }

                    var stored = await Store(blockGraph);
                    if (stored == null)
                    {
                        logger.LogError($"<<< AddBlockGraph >>>: Could not store graph to mem pool for block {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                        return null;
                    }

                    return stored;
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.AddBlockGraph >>>: {ex.ToString()}");
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> AddToSelf(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            BlockGraphProto stored = null;

            try
            {
                var can = await CanAdd(blockGraph, httpService.NodeIdentity);
                if (can == null)
                {
                    return blockGraph;
                }

                round += 1;

                var signed = await signingProvider.Sign(httpService.NodeIdentity, blockGraph, round, await httpService.GetPublicKey());
                var prev = await unitOfWork.BlockGraph.GetPrevious(httpService.NodeIdentity, round);

                signed.Prev = prev?.Block;

                stored = await Store(signed);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.AddToSelf >>>: {ex.ToString()}");
            }

            return stored;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<long> NetworkBlockHeight()
        {
            long height = 0;
            List<long> list = new List<long>();

            try
            {
                var responses = await httpService.Dial(DialType.Get, "height");
                foreach (var response in responses)
                {
                    var jToken = Util.ReadJToken(response, "height");
                    list.Add(jToken.Value<long>());
                }

                if (list.Any())
                {
                    height = list.Max();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.NetworkBlockHeight >>>: {ex.ToString()}");
            }

            return height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public bool ValidateRule(CoinProto coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var coinHasElements = coin.Validate().Any();
            if (!coinHasElements)
            {
                try
                {
                    using var secp256k1 = new Secp256k1();
                    using var bulletProof = new BulletProof();

                    var success = bulletProof.Verify(coin.Commitment.FromHex(), coin.RangeProof.FromHex(), null);
                    if (!success)
                        return false;
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.ValidateRule >>>: {ex.ToString()}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIDProto"></param>
        /// <returns></returns>
        public bool VerifiySignature(BlockIDProto blockIDProto)
        {
            if (blockIDProto == null)
                throw new ArgumentNullException(nameof(blockIDProto));

            bool result = false;

            try
            {
                var signedBlock = blockIDProto.SignedBlock;
                var blockHash = signingProvider.BlockHash(signedBlock.Coin.Stamp, blockIDProto.Node, blockIDProto.Round, signedBlock.PublicKey);
                var coinHash = signingProvider.HashCoin(signedBlock.Coin, signedBlock.PublicKey);
                var combinedHash = Util.Combine(blockHash, coinHash);

                result = Ed25519.Verify(signedBlock.Signature.FromHex(), combinedHash, signedBlock.PublicKey.FromHex());
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.VerifiySignature >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// Verifiies the coin chain.
        /// </summary>
        /// <returns><c>true</c>, if coin chain was verifiyed, <c>false</c> otherwise.</returns>
        /// <param name="previous">Previous coin</param>
        /// <param name="next">Next coin.</param>
        public bool VerifiyHashChain(CoinProto previous, CoinProto next)
        {
            if (previous == null)
                throw new ArgumentNullException(nameof(previous));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            bool validH = false, validK = false;

            try
            {
                var hint = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp} {next.Principle}").ToHex();
                var keeper = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp} {next.Hint}").ToHex();

                validH = previous.Hint.Equals(hint);
                validK = previous.Keeper.Equals(keeper);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.VerifiyHashChain >>>: {ex.ToString()}");
            }

            return validH && validK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<int> BlockHeight()
        {
            int blockHeight = 0;

            try
            {
                blockHeight = await unitOfWork.BlockID.Count(httpService.NodeIdentity);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.BlockHeight >>>: {ex.ToString()}");
            }

            return blockHeight;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public async Task<bool> InterpretBlocks(IEnumerable<BlockID> blocks)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            using (await interpretBlocksMutex.LockAsync())
            {
                foreach (var block in blocks)
                {
                    var coinExists = await unitOfWork.BlockID.HasCoin(block.SignedBlock.Coin.Commitment);
                    if (coinExists)
                    {
                        logger.LogWarning($"<<< BlockGraphService.InterpretBlocks >>>: Coin exists for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var blockIdProto = new BlockIDProto { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                    if (!VerifiySignature(blockIdProto))
                    {
                        logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: unable to verify signature for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var coinRule = ValidateRule(blockIdProto.SignedBlock.Coin);
                    if (!coinRule)
                    {
                        logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var coins = await unitOfWork.BlockID.GetManyCoins(blockIdProto.SignedBlock.Coin.Stamp, httpService.NodeIdentity);
                    if (coins?.Any() == true)
                    {
                        var list = coins.ToList();
                        for (int i = 0; i < list.Count; i++)
                        {
                            CoinProto previous;
                            CoinProto next;

                            try
                            {
                                previous = list[(i - 1) % (list.Count - 1)].SignedBlock.Coin;
                            }
                            catch (DivideByZeroException)
                            {
                                previous = list[i].SignedBlock.Coin;
                            }

                            var previousRule = ValidateRule(previous);
                            if (!previousRule)
                            {
                                logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                return false;
                            }

                            try
                            {
                                next = list[(i + 1) % (list.Count - 1)].SignedBlock.Coin;

                                var nextRule = ValidateRule(next);
                                if (!nextRule)
                                {
                                    logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                    return false;
                                }
                            }
                            catch (DivideByZeroException)
                            {
                                next = blockIdProto.SignedBlock.Coin;
                            }

                            if (!VerifiyHashChain(previous, next))
                            {
                                logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: Could not verify hash chain for Interpreted BlockID");
                                return false;
                            }
                        }

                        using var pedersen = new Pedersen();

                        var sum = coins.Select(c => c.SignedBlock.Coin.Commitment.FromHex());
                        var success = pedersen.VerifyCommitSum(new List<byte[]> { sum.First() }, sum.Skip(1));
                        if (!success)
                        {
                            logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: Could not verify committed sum for Interpreted BlockID");
                            return false;
                        }
                    }

                    var blockId = await unitOfWork.BlockID.StoreOrUpdate(blockIdProto);
                    if (blockId == null)
                    {
                        logger.LogError($"<<< BlockGraphService.InterpretBlocks >>>: Could not save block for {blockIdProto.Node} and round {blockIdProto.Round}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> Store(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            BlockGraphProto stored = null;

            try
            {
                var can = await CanAdd(blockGraph, blockGraph.Block.Node);
                if (can == null)
                {
                    return blockGraph;
                }

                stored = await unitOfWork.BlockGraph.StoreOrUpdate(new BlockGraphProto
                {
                    Block = blockGraph.Block,
                    Deps = blockGraph.Deps?.Select(d => d).ToList(),
                    Prev = blockGraph.Prev ?? null,
                    Included = blockGraph.Included,
                    Replied = blockGraph.Replied
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.Store >>>: {ex.ToString()}");
            }

            return stored;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> CanAdd(BlockGraphProto blockGraph, ulong node)
        {
            var blockGraphs = await unitOfWork.BlockGraph.GetMany(blockGraph.Block.Hash, node);
            if (blockGraphs.Any())
            {
                var graph = blockGraphs.FirstOrDefault(x => x.Block.Round.Equals(blockGraph.Block.Round));
                if (graph != null)
                {
                    logger.LogWarning($"<<< BlockGraphService.CanAdd >>>: Block exists for node {graph.Block.Node} and round {graph.Block.Round}");
                    return null;
                }
            }

            return blockGraph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interpreted"></param>
        private void BlockmaniaCallback(Interpreted interpreted)
        {
            if (interpreted == null)
                throw new ArgumentNullException(nameof(interpreted));

            unitOfWork.Interpreted.Store(interpreted.Consumed, interpreted.Round);

            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var interpretedList = new List<BlockID>();

                    foreach (var block in interpreted.Blocks)
                    {
                        var blockGraphs = await unitOfWork.BlockGraph.GetMany(block.Hash, httpService.NodeIdentity);
                        if (blockGraphs.Any() != true)
                        {
                            logger.LogWarning($"<<< BlockGraphService.BlockmaniaCallback >>>: Unable to find blocks with - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                            continue;
                        }

                        var blockGraph = blockGraphs.FirstOrDefault(x => x.Block.Node.Equals(httpService.NodeIdentity) && x.Block.Round.Equals(block.Round));
                        if (blockGraph == null)
                        {
                            logger.LogError($"<<< BlockGraphService.BlockmaniaCallback >>>: Unable to find matching block - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                            continue;
                        }

                        interpretedList.Add(new BlockID(blockGraph.Block.Hash, blockGraph.Block.Node, blockGraph.Block.Round, blockGraph.Block.SignedBlock));
                    }

                    // Should return success blocks instead of bool.
                    var success = await InterpretBlocks(interpretedList);
                    if (success)
                    {
                        await dataProvider.MarkAs(interpretedList, JobState.Polished);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.BlockmaniaCallback >>>: {ex.ToString()}");
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task ProcessBlocks()
        {
            using (await processBlocksMutex.LockAsync())
            {
                processBlocksTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

                try
                {
                    while (dataProvider.DataQueue.TryDequeue(out BlockGraphProto x))
                    {
                        if (!VerifiySignature(x.Block))
                        {
                            logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Unable to verify signature for block {x.Block.Round} from node {x.Block.Node}");
                            return;
                        }

                        if (x.Prev != null && x.Prev?.Round != 0)
                        {
                            if (!VerifiySignature(x.Prev))
                            {
                                logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Unable to verify signature for previous block on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (x.Prev.Node != x.Block.Node)
                            {
                                logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Previous block node does not match on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (x.Prev.Round + 1 != x.Block.Round)
                            {
                                logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Previous block round is invalid on block {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }
                        }

                        for (int i = 0; i < x.Deps.Count(); i++)
                        {
                            var dep = x.Deps[i];

                            if (!VerifiySignature(dep.Block))
                            {
                                logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Unable to verify signature for block reference {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }

                            if (dep.Block.Node == x.Block.Node)
                            {
                                logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Block references includes a block from same node in block reference  {x.Block.Round} from node {x.Block.Node}");
                                return;
                            }
                        }

                        Graph.Add(x.ToBlockGraph());
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: {ex.ToString()}");
                }
                finally
                {
                    processBlocksTimer.Change(processBlocksInterval, System.Threading.Timeout.Infinite);
                }
            }
        }
    }
}
