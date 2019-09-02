using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Model;
using Core.API.Helper;
using Microsoft.Extensions.Logging;
using Core.API.Onion;
using System.Net.Http;

namespace Coin.API.Services
{
    public class CoinService : ICoinService
    {
        private readonly IBlockGraphService blockGraphService;
        private readonly IUnitOfWork unitOfWork;
        private readonly ITorClient torClient;
        private readonly ILogger logger;

        public CoinService(IBlockGraphService blockGraphService, IUnitOfWork unitOfWork, ITorClient torClient, ILogger<CoinService> logger)
        {
            this.blockGraphService = blockGraphService;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
            this.torClient = torClient;
        }

        /// <summary>
        /// Adds the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="coin">Coin.</param>
        public async Task<byte[]> AddCoin(byte[] coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            try
            {
                var coinProto = Util.DeserializeProto<CoinProto>(coin);
                var coinHasElements = coinProto.Validate().Any();

                if (!coinHasElements)
                {
                    var blockId = await unitOfWork.BlockID.Get(coinProto.Stamp.ToStr().FromHex());
                    if (blockId != null)
                    {
                        logger.LogError($"BlockGraphService: BlockID exists");
                        return null;
                    }

                    var signedBlockGraph = await blockGraphService.Sign(coinProto, 1);
                    if (signedBlockGraph == null)
                    {
                        return null;
                    }

                    var addedBlockGraph = await blockGraphService.AddBlockGraph(signedBlockGraph);
                    if (addedBlockGraph == null)
                    {
                        return null;
                    }

                    Broadcast(coin);

                    var block = Util.SerializeProto(new BlockIDProto
                    {
                        Hash = addedBlockGraph.Block.Hash,
                        Node = addedBlockGraph.Block.Node,
                        Round = addedBlockGraph.Block.Round,
                        SignedBlock = new BlockProto
                        {
                            Coin = addedBlockGraph.Block.SignedBlock.Coin,
                            Key = addedBlockGraph.Block.SignedBlock.Key.ToStr().ToBytes(),
                            PublicKey = addedBlockGraph.Block.SignedBlock.PublicKey.ToHex().ToBytes(),
                            Signature = addedBlockGraph.Block.SignedBlock.Signature.ToHex().ToBytes()
                        }
                    });

                    return block;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets the coin.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="address">Key.</param>
        public async Task<byte[]> GetCoin(byte[] address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            byte[] blockIdByte = null;

            try
            {
                var blockId = await unitOfWork.BlockID.Get(address);
                if (blockId != null)
                {
                    blockIdByte = Util.SerializeProto(blockId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return blockIdByte;
        }

        /// <summary>
        /// Gets the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        public async Task<byte[]> GetCoins(byte[] key, int skip, int take)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                result = await GetCoins(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Gets the coins.
        /// </summary>
        /// <returns>The coins.</returns>
        /// <param name="key">Key.</param>
        public async Task<byte[]> GetCoins(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var blockIds = await unitOfWork.BlockID.Search(key);
                if (blockIds?.Any() == true)
                {
                    result = Util.SerializeProto(blockIds);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        private void Broadcast(byte[] coin)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                var tasks = new List<Task<HttpResponseMessage>>();
                var endPoints = blockGraphService.Endpoints().Where(x => !x.Equals(blockGraphService.Hostname));

                for (int ep = 0; ep < endPoints.Count(); ep++)
                {
                    var uri = new Uri(new Uri(endPoints.ElementAt(ep)), "coin");
                    tasks.Add(torClient.PostAsJsonAsync(uri, coin));
                }

                await Task.WhenAll(tasks);
            });
        }
    }
}
