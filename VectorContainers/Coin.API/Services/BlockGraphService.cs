using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class BlockGraphService : IBlockGraphService
    {
        private const int processBlocksInterval = 1 * 60 * 32;
        private const int requiredNodeCount = 4;

        private static readonly AsyncLock processBlocksMutex = new AsyncLock();
        private static readonly AsyncLock addBlockGraphMutex = new AsyncLock();

        private Graph Graph;
        private Config Config;

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ILogger logger;

        private readonly HierarchicalDataProvider dataProvider;
        private readonly SigningProvider signingProvider;
        private readonly InterpretBlocksProvider interpretBlocksProvider;

        private Timer processBlocksTimer;
        private ulong lastInterpreted;
        private ulong round;

        public BlockGraphService(IUnitOfWork unitOfWork, IHttpService httpService, HierarchicalDataProvider dataProvider, SigningProvider signingProvider,
            InterpretBlocksProvider interpretBlocksProvider, ILogger<BlockGraphService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.dataProvider = dataProvider;
            this.signingProvider = signingProvider;
            this.interpretBlocksProvider = interpretBlocksProvider;
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
            var blockGraphs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(blockGraph.Block.Hash) && x.Block.Node.Equals(node));
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
                        var blockGraphs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(block.Hash) && x.Block.Node.Equals(httpService.NodeIdentity));
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
                    var success = await interpretBlocksProvider.Interpret(interpretedList);
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
                        if (!signingProvider.VerifiySignature(x.Block))
                        {
                            logger.LogError($"<<< BlockGraphService.ProcessBlocks >>>: Unable to verify signature for block {x.Block.Round} from node {x.Block.Node}");
                            return;
                        }

                        if (x.Prev != null && x.Prev?.Round != 0)
                        {
                            if (!signingProvider.VerifiySignature(x.Prev))
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

                            if (!signingProvider.VerifiySignature(dep.Block))
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
