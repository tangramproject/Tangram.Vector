// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TGMNode.Model;
using TGMCore.Services;
using TGMCore.Model;
using TGMCore.Providers;

namespace TGMNode.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IBlockGraphService<TransactionProto> _blockGraphService;
        private readonly ILogger _logger;
        private readonly IBaseGraphRepository<TransactionProto> _baseGraphRepository;
        private readonly IBaseBlockIDRepository<TransactionProto> _baseBlockIDRepository;
        private readonly IClusterProvider _clusterProvider;

        public TransactionService(IUnitOfWork unitOfWork, IBlockGraphService<TransactionProto> blockGraphService,
            IClusterProvider clusterProvider, ILogger<TransactionService> logger)
        {
            _blockGraphService = blockGraphService;
            _clusterProvider = clusterProvider;
            _logger = logger;
            _baseGraphRepository = unitOfWork.CreateBaseGraphOf<TransactionProto>();
            _baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<TransactionProto>();
        }

        /// <summary>
        /// Add transaction
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public async Task<byte[]> AddTransaction(TransactionProto tx)
        {
            if (tx == null)
                throw new ArgumentNullException(nameof(tx));

            try
            {
                var coinHasElements = tx.Validate().Any();
                if (!coinHasElements)
                {
                    var blockIDExist = await _baseBlockIDRepository
                        .GetFirstOrDefault(x => x.SignedBlock.Attach.Vin.K == tx.Vin.K && x.SignedBlock.Attach.Version == tx.Version);

                    if (blockIDExist != null)
                    {
                        return null;
                    }

                    var blockGraphExist = await _baseGraphRepository.GetFirstOrDefault(x =>
                        x.Block.Hash == tx.Vin.K &&
                        x.Block.SignedBlock.Attach.Version == tx.Version &&
                        x.Block.Node == _clusterProvider.GetSelfUniqueAddress());

                    if (blockGraphExist != null)
                    {
                        return null;
                    }

                    var blockGraph = new BaseGraphProto<TransactionProto>
                    {
                        Block = new BaseBlockIDProto<TransactionProto>
                        {
                            Node = _clusterProvider.GetSelfUniqueAddress(),
                            Hash = tx.Vin.K,
                            SignedBlock = new BaseBlockProto<TransactionProto> { Attach = tx, Key = tx.Vin.K }
                        },
                        Deps = new List<DepProto<TransactionProto>>()
                    };

                    var graphProto = await _blockGraphService.SetBlockGraph(blockGraph);
                    if (graphProto == null)
                    {
                        return null;
                    }

                    var block = TGMCore.Helper.Util.SerializeProto(graphProto.Block);
                    return block;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< TransactionService.AddTransaction >>>: {ex}");
            }

            return null;
        }

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <returns>The coin.</returns>
        /// <param name="key">Key.</param>
        public async Task<byte[]> GetTransaction(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var blockId = await _baseBlockIDRepository.GetFirstOrDefault(x => x.SignedBlock.Attach.PreImage.Equals(key));
                if (blockId != null)
                {
                    result = TGMCore.Helper.Util.SerializeProto(blockId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< TransactionService.GetTransaction >>>: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Gets the transactions.
        /// </summary>
        /// <returns>List of transactions.</returns>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        public async Task<byte[]> GetTransactions(string key, int skip, int take)
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
                result = await GetTransactions(key);
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< TransactionService.GetTransactions >>>: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<byte[]> GetTransactions(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            byte[] result = null;

            try
            {
                var blockIds = await _baseBlockIDRepository.GetRange(skip, take);
                if (blockIds?.Any() == true)
                {
                    result = TGMCore.Helper.Util.SerializeProto(blockIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< TransactionService.GetCoins >>>: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Gets transactions.
        /// </summary>
        /// <returns>List of transactions.</returns>
        /// <param name="key">Key.</param>
        public async Task<byte[]> GetTransactions(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            byte[] result = null;

            try
            {
                var blockIds = await _baseBlockIDRepository.GetWhere(x => x.SignedBlock.Attach.PreImage.Equals(key));
                if (blockIds?.Any() == true)
                {
                    result = TGMCore.Helper.Util.SerializeProto(blockIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< TransactionService.GetTransactions >>>: {ex}");
            }

            return result;
        }
    }
}
