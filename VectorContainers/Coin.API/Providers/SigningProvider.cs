using System;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Model;
using Core.API.Onion;
using Core.API.Signatures;
using Microsoft.Extensions.Logging;
using Secp256k1_ZKP.Net;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public bool ValidateRule(CoinProto coin)
        {
            if (coin == null)
                throw new ArgumentNullException(nameof(coin));

            var coinHasElements = coin.Validate().Any();
            if (!coinHasElements)
            {
                try
                {
                    using var secp256k1 = new Secp256k1();
                    using var bulletProof = new BulletProof();

                    var success = bulletProof.Verify(coin.Commitment.FromHex(), coin.RangeProof.FromHex(), null);
                    if (!success)
                        return false;
                }
                catch (Exception ex)
                {
                    logger.LogError($"<<< BlockGraphService.ValidateRule >>>: {ex.ToString()}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIDProto"></param>
        /// <returns></returns>
        public bool VerifiySignature(BlockIDProto blockIDProto)
        {
            if (blockIDProto == null)
                throw new ArgumentNullException(nameof(blockIDProto));

            bool result = false;

            try
            {
                var signedBlock = blockIDProto.SignedBlock;
                var blockHash = BlockHash(signedBlock.Coin.Stamp, blockIDProto.Node, blockIDProto.Round, signedBlock.PublicKey);
                var coinHash = HashCoin(signedBlock.Coin, signedBlock.PublicKey);
                var combinedHash = Util.Combine(blockHash, coinHash);

                result = Ed25519.Verify(signedBlock.Signature.FromHex(), combinedHash, signedBlock.PublicKey.FromHex());
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.VerifiySignature >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// Verifiies the coin chain.
        /// </summary>
        /// <returns><c>true</c>, if coin chain was verifiyed, <c>false</c> otherwise.</returns>
        /// <param name="previous">Previous coin</param>
        /// <param name="next">Next coin.</param>
        public bool VerifiyHashChain(CoinProto previous, CoinProto next)
        {
            if (previous == null)
                throw new ArgumentNullException(nameof(previous));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            bool validH = false, validK = false;

            try
            {
                var hint = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp} {next.Principle}").ToHex();
                var keeper = Cryptography.GenericHashNoKey($"{next.Version} {next.Stamp} {next.Hint}").ToHex();

                validH = previous.Hint.Equals(hint);
                validK = previous.Keeper.Equals(keeper);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphService.VerifiyHashChain >>>: {ex.ToString()}");
            }

            return validH && validK;
        }
    }
}
