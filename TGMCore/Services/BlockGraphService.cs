// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using TGMCore.Providers;
using TGMCore.Extentions;
using TGMCore.Messages;
using TGMCore.Model;
using Microsoft.Extensions.Logging;

namespace TGMCore.Services
{
    public class BlockGraphService<TAttach> : IBlockGraphService<TAttach>
    {
        private readonly IGraphActorProvider<TAttach> _graphActorProvider;
        private readonly ILogger _logger;
        private readonly IBaseGraphRepository<TAttach> _baseGraphRepository;

        public BlockGraphService(IGraphActorProvider<TAttach> graphActorProvider, IUnitOfWork unitOfWork, ILogger<BlockGraphService<TAttach>> logger)
        {
            _graphActorProvider = graphActorProvider;
            _baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public async Task<bool> HasKeyImage(byte[] hash)
        {
            BaseGraphProto<TAttach> blockGraph = null;

            try
            {
                blockGraph = await  _baseGraphRepository.GetFirstOrDefault(x => x.Block.Hash == hash.ToHex());
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< BlockGraphService.HasKeyImage >>>: {ex}");
            }

            return blockGraph == null ? false : true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        public async Task<BaseGraphProto<TAttach>> SetBlockGraph(BaseGraphProto<TAttach> blockGraph)
        {
            if (blockGraph == null)
                return null;

            BaseGraphProto<TAttach> stored = null;

            try
            {
                var can = await _baseGraphRepository.CanAdd(blockGraph, blockGraph.Block.Node);
                if (can == null)
                {
                    return blockGraph;
                }

                stored = await _baseGraphRepository.StoreOrUpdate(new BaseGraphProto<TAttach>
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

                await _graphActorProvider.RegisterAsync(new HashedMessage(stored.Block.Hash.FromHex()));
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< BlockGraphService.SetBlockGraph >>>: {ex}");
            }

            return stored;
        }
    }
}
