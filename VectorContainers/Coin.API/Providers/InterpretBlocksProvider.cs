using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coin.API.Services;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;
using Secp256k1_ZKP.Net;

namespace Coin.API.Providers
{
    public class InterpretBlocksProvider
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly SigningProvider signingProvider;
        private readonly ILogger logger;

        private static readonly AsyncLock interpretBlocksMutex = new AsyncLock();

        public InterpretBlocksProvider(IUnitOfWork unitOfWork, IHttpService httpService, SigningProvider signingProvider, ILogger<InterpretBlocksProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.signingProvider = signingProvider;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public async Task<bool> Interpret(IEnumerable<BlockID> blocks)
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
                        logger.LogWarning($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Coin exists for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var blockIdProto = new BlockIDProto { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                    if (!signingProvider.VerifiySignature(blockIdProto))
                    {
                        logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to verify signature for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var coinRule = signingProvider.ValidateRule(blockIdProto.SignedBlock.Coin);
                    if (!coinRule)
                    {
                        logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                        continue;
                    }

                    var coins = await unitOfWork.BlockID
                        .GetWhere(x => x.SignedBlock.Coin.Stamp.Equals(blockIdProto.SignedBlock.Coin.Stamp) && x.Node.Equals(httpService.NodeIdentity));

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

                            var previousRule = signingProvider.ValidateRule(previous);
                            if (!previousRule)
                            {
                                logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                return false;
                            }

                            try
                            {
                                next = list[(i + 1) % (list.Count - 1)].SignedBlock.Coin;

                                var nextRule = signingProvider.ValidateRule(next);
                                if (!nextRule)
                                {
                                    logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                    return false;
                                }
                            }
                            catch (DivideByZeroException)
                            {
                                next = blockIdProto.SignedBlock.Coin;
                            }

                            if (!signingProvider.VerifiyHashChain(previous, next))
                            {
                                logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not verify hash chain for Interpreted BlockID");
                                return false;
                            }
                        }

                        using var pedersen = new Pedersen();

                        var sum = coins.Select(c => c.SignedBlock.Coin.Commitment.FromHex());
                        var success = pedersen.VerifyCommitSum(new List<byte[]> { sum.First() }, sum.Skip(1));
                        if (!success)
                        {
                            logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not verify committed sum for Interpreted BlockID");
                            return false;
                        }
                    }

                    var blockId = await unitOfWork.BlockID.StoreOrUpdate(blockIdProto);
                    if (blockId == null)
                    {
                        logger.LogError($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not save block for {blockIdProto.Node} and round {blockIdProto.Round}");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
