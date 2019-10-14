using System;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Model;
using Core.API.Onion;
using Microsoft.Extensions.Logging;

namespace Coin.API.Providers
{
    public class SigningProvider
    {
        private readonly IOnionServiceClient onionServiceClient;
        private readonly ILogger logger;

        public SigningProvider(IOnionServiceClient onionServiceClient, ILogger<SigningProvider> logger)
        {
            this.onionServiceClient = onionServiceClient;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="blockGraph"></param>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public async Task<BlockGraphProto> Sign(ulong nodeId, BlockGraphProto blockGraph, ulong round, byte[] publicKey)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            if (round <= 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            try
            {
                var blockHash = BlockHash(blockGraph.Block.SignedBlock.Coin.Stamp, nodeId, round, publicKey.ToHex());
                var coinHash = HashCoin(blockGraph.Block.SignedBlock.Coin, publicKey.ToHex());
                var combinedHash = Util.Combine(blockHash, coinHash);
                var signedHash = await onionServiceClient.SignHashAsync(combinedHash);

                var signed = new BlockGraphProto
                {
                    Block = new BlockIDProto
                    {
                        Hash = blockGraph.Block.SignedBlock.Coin.Stamp,
                        Node = nodeId,
                        Round = round,
                        SignedBlock = new BlockProto
                        {
                            Key = blockGraph.Block.SignedBlock.Coin.Stamp,
                            Coin = blockGraph.Block.SignedBlock.Coin,
                            PublicKey = signedHash.PublicKey.ToHex(),
                            Signature = signedHash.Signature.ToHex()
                        }
                    }
                };

                return signed;
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SigningProvider.Sign >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stamp"></param>
        /// <param name="node"></param>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public byte[] BlockHash(string stamp, ulong node, ulong round, string publicKey)
        {
            if (string.IsNullOrEmpty(stamp))
                throw new ArgumentNullException(nameof(stamp));

            if (node <= 0)
                throw new ArgumentOutOfRangeException(nameof(node));

            if (round <= 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException(nameof(publicKey));

            byte[] hash = null;

            try
            {
                hash = Cryptography.GenericHashWithKey($"{stamp}{node}{round}", publicKey.FromHex());
            }
            catch (Exception ex)
            {
                logger.LogWarning($"<<< SigningProvider.BlockHash >>>: {ex.ToString()}");
            }

            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] HashCoin(CoinProto coin, string key = null)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            byte[] hash = null;

            try
            {
                var serialized = Util.SerializeProto(coin);

                hash = key == null ? Cryptography.GenericHashNoKey(serialized) : Cryptography.GenericHashWithKey(serialized, key.FromHex());
            }
            catch (Exception ex)
            {
                logger.LogWarning($"<<< SigningProvider.HashCoin >>>: {ex.ToString()}");
            }

            return hash;
        }
    }
}
