// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using TGMCore.Providers;
using TGMCore.Extentions;
using TGMCore.Helper;
using TGMCore.Messages;
using TGMCore.Model;
using Microsoft.Extensions.Logging;

namespace TGMCore.Services
{
    public class BlockGraphService<TAttach> : IBlockGraphService<TAttach>
    {
        private static readonly AsyncLock _setBlockGraphMutex = new AsyncLock();

        private readonly ISipActorProvider _sipActorProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;
        private readonly IBaseGraphRepository<TAttach> baseGraphRepository;

        public BlockGraphService(ISipActorProvider sipActorProvider, IUnitOfWork unitOfWork, ILogger<BlockGraphService<TAttach>> logger)
        {
            _sipActorProvider = sipActorProvider;
            _unitOfWork = unitOfWork;
            _logger = logger;

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

            using (await _setBlockGraphMutex.LockAsync())
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
                        _logger.LogError($"<<< BlockGraphService.SetBlockGraph >>>: Unable to save block {blockGraph.Block.Hash} for round {blockGraph.Block.Round} and node {blockGraph.Block.Node}");
                        return null;
                    }

                    _sipActorProvider.Register(new HashedMessage(stored.Block.Hash.FromHex()));

                    return stored;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"<<< BlockGraphService.SetBlockGraph >>>: {ex.ToString()}");
                }
            }

            return null;
        }
    }
}
