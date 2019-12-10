using System;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Model;
using Core.API.Helper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Core.API.Network;
using Coin.API.Model;
using Core.API.Services;

namespace Coin.API.Services
{
    public class CoinService : ICoinService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IBlockGraphService<CoinProto> blockGraphService;
        private readonly IHttpClientService httpClientService;
        private readonly ILogger logger;
        private readonly IBaseGraphRepository<CoinProto> baseGraphRepository;
        private readonly IBaseBlockIDRepository<CoinProto> baseBlockIDRepository;

        public CoinService(IUnitOfWork unitOfWork, IBlockGraphService<CoinProto> blockGraphService, IHttpClientService httpClientService, ILogger<CoinService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.blockGraphService = blockGraphService;
            this.httpClientService = httpClientService;
            this.logger = logger;

            baseGraphRepository = unitOfWork.CreateBaseGraphOf<CoinProto>();
            baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<CoinProto>();
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
                    var blockIDExist = await baseBlockIDRepository
                        .GetFirstOrDefault(x => x.SignedBlock.Attach.Stamp.Equals(coin.Stamp) && x.SignedBlock.Attach.Version.Equals(coin.Version));

                    if (blockIDExist != null)
                    {
                        return null;
                    }

                    var blockGraphExist = await baseGraphRepository.GetFirstOrDefault(x =>
                        x.Block.Hash.Equals(coin.Stamp) &&
                        x.Block.SignedBlock.Attach.Version.Equals(coin.Version) &&
                        x.Block.Node.Equals(httpClientService.NodeIdentity));

                    if (blockGraphExist != null)
                    {
                        return null;
                    }

                    var blockGraph = new BaseGraphProto<CoinProto>
                    {
                        Block = new BaseBlockIDProto<CoinProto>
                        {
                            Node = httpClientService.NodeIdentity,
                            Hash = coin.Stamp,
                            SignedBlock = new BaseBlockProto<CoinProto> { Attach = coin, Key = coin.Stamp }
                        },
                        Deps = new List<DepProto<CoinProto>>()
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
                var blockId = await baseBlockIDRepository.GetFirstOrDefault(x => x.SignedBlock.Attach.Hash.Equals(key));
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
                var blockIds = await baseBlockIDRepository.GetRange(skip, take);
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
                var blockIds = await baseBlockIDRepository.GetWhere(x => x.SignedBlock.Attach.Hash.Equals(key));
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
