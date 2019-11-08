using System;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Model;
using Core.API.Helper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Coin.API.Services
{
    public class CoinService : ICoinService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IBlockGraphService blockGraphService;
        private readonly IHttpService httpService;
        private readonly ILogger logger;

        public CoinService(IUnitOfWork unitOfWork, IBlockGraphService blockGraphService, IHttpService httpService, ILogger<CoinService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.blockGraphService = blockGraphService;
            this.httpService = httpService;
            this.logger = logger;
        }

        /// <summary>
        /// Adds the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="coin">Coin.</param>
        public async Task<byte[]> AddCoin(CoinProto coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            try
            {
                var coinHasElements = coin.Validate().Any();
                if (!coinHasElements)
                {
                    var blockIDExist = await unitOfWork.BlockID.HasCoin(coin.Stamp, coin.Version);
                    if (blockIDExist)
                    {
                        return null;
                    }

                    var blockGraphExist = await unitOfWork.BlockGraph.GetFirstOrDefault(x =>
                        x.Block.Hash.Equals(coin.Stamp) &&
                        x.Block.SignedBlock.Coin.Version.Equals(coin.Version) &&
                        x.Block.Node.Equals(httpService.NodeIdentity));

                    if (blockGraphExist != null)
                    {
                        return null;
                    }

                    var blockGraph = new BlockGraphProto
                    {
                        Block = new BlockIDProto
                        {
                            Node = httpService.NodeIdentity,
                            Hash = coin.Stamp,
                            SignedBlock = new BlockProto { Coin = coin, Key = coin.Stamp }
                        },
                        Deps = new List<DepProto>()
                    };

                    var graphProto = await blockGraphService.SetBlockGraph(blockGraph);
                    if (graphProto == null)
                    {
                        return null;
                    }

                    var block = Util.SerializeProto(graphProto.Block);
                    return block;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< CoinService.AddCoin >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// Gets the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="key">Key.</param>
        public async Task<byte[]> GetCoin(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var blockId = await unitOfWork.BlockID.GetFirstOrDefault(x => x.SignedBlock.Coin.Hash.Equals(key));
                if (blockId != null)
                {
                    result = Util.SerializeProto(blockId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< CoinService.GetCoin >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// Gets the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        public async Task<byte[]> GetCoins(string key, int skip, int take)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            byte[] result = null;

            try
            {
                result = await GetCoins(key);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< CoinService.GetCoins >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<byte[]> GetCoins(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            byte[] result = null;

            try
            {
                var blockIds = await unitOfWork.BlockID.GetRange(skip, take);
                if (blockIds?.Any() == true)
                {
                    result = Util.SerializeProto(blockIds);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< CoinService.GetCoins >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// Gets the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="key">Key.</param>
        public async Task<byte[]> GetCoins(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var blockIds = await unitOfWork.BlockID.GetWhere(x => x.SignedBlock.Coin.Hash.Equals(key));
                if (blockIds?.Any() == true)
                {
                    result = Util.SerializeProto(blockIds);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< CoinService.GetCoins >>>: {ex.ToString()}");
            }

            return result;
        }
    }
}
