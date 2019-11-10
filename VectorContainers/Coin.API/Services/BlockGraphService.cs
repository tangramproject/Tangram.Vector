using System;
using System.Linq;
using System.Threading.Tasks;
using Coin.API.ActorProviders;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class BlockGraphService : IBlockGraphService
    {
        private static readonly AsyncLock setBlockGraphMutex = new AsyncLock();

        private readonly ISipActorProvider sipActorProvider;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger logger;

        public BlockGraphService(ISipActorProvider sipActorProvider, IUnitOfWork unitOfWork, ILogger<BlockGraphService> logger)
        {
            this.sipActorProvider = sipActorProvider;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> SetBlockGraph(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            using (await setBlockGraphMutex.LockAsync())
            {
                try
                {
                    var can = await unitOfWork.BlockGraph.CanAdd(blockGraph, blockGraph.Block.Node);
                    if (can == null)
                    {
                        return blockGraph;
                    }

                    var stored = await unitOfWork.BlockGraph.StoreOrUpdate(new BlockGraphProto
                    {
                        Block = blockGraph.Block,
                        Deps = blockGraph.Deps?.Select(d => d).ToList(),
                        Prev = blockGraph.Prev ?? null,
                        Included = blockGraph.Included,
                        Replied = blockGraph.Replied
                    });

                    if (stored == null)
                    {
                        logger.LogError($"<<< BlockGraphService.SetBlockGraph >>>: Unable to save block {blockGraph.Block.Hash} for round {blockGraph.Block.Round} and node {blockGraph.Block.Node}");
                        return null;
                    }

                    sipActorProvider.Register(new HashedMessage(stored.Block.Hash.FromHex()));

                    return stored;
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.SetBlockGraph >>>: {ex.ToString()}");
                }
            }

            return null;
        }
    }
}
