using System;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Actors.Providers;
using Core.API.Extentions;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Core.API.Services
{
    public class BlockGraphService<TAttach> : IBlockGraphService<TAttach>
    {
        private static readonly AsyncLock setBlockGraphMutex = new AsyncLock();

        private readonly ISipActorProvider sipActorProvider;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger logger;
        private readonly IBaseGraphRepository<TAttach> baseGraphRepository;

        public BlockGraphService(ISipActorProvider sipActorProvider, IUnitOfWork unitOfWork, ILogger<BlockGraphService<TAttach>> logger)
        {
            this.sipActorProvider = sipActorProvider;
            this.unitOfWork = unitOfWork;
            this.logger = logger;

            baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        public async Task<BaseGraphProto<TAttach>> SetBlockGraph(BaseGraphProto<TAttach> blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            using (await setBlockGraphMutex.LockAsync())
            {
                try
                {
                    var can = await baseGraphRepository.CanAdd(blockGraph, blockGraph.Block.Node);
                    if (can == null)
                    {
                        return blockGraph;
                    }


                    var stored = await baseGraphRepository.StoreOrUpdate(new BaseGraphProto<TAttach>
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
