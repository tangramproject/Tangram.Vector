using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Onion;
using Core.API.Signatures;
using Secp256k1_ZKP.Net;

namespace Coin.API.Actors
{
    public class SigningActor : ReceiveActor
    {
        private readonly IOnionServiceClient onionServiceClient;
        private readonly ILoggingAdapter logger;

        public SigningActor(IOnionServiceClient onionServiceClient)
        {
            this.onionServiceClient = onionServiceClient;

            logger = Context.GetLogger();

            Receive<ValidateCoinRuleMessage>(message => Sender.Tell(ValidateCoinRule(message)));

            Receive<VerifiyBlockSignatureMessage>(message => Sender.Tell(VerifiyBlockSignature(message)));

            Receive<VerifiySignatureMessage>(message => Sender.Tell(VerifiySignature(message)));

            Receive<VerifiyHashChainMessage>(message => Sender.Tell(VerifiyHashChain(message)));

            ReceiveAsync<SignedHashMessage>(async message => Sender.Tell(await Sign(message)));

            ReceiveAsync<SignedBlockGraphMessage>(async message => Sender.Tell(await Sign(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> Sign(SignedBlockGraphMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.BlockGraph == null)
                throw new ArgumentNullException(nameof(message.BlockGraph));

            if (message.Round <= 0)
                throw new ArgumentOutOfRangeException(nameof(message.Round));

            try
            {
                var blockHash = BlockHash(new SignedBlockHashMessage(message.BlockGraph.Block.SignedBlock.Coin.Stamp, message.Node, message.Round, message.PublicKey));
                var coinHash = HashCoin(new SignedHashCoinMessage(message.BlockGraph.Block.SignedBlock.Coin, message.PublicKey));
                var combinedHash = Util.Combine(blockHash, coinHash);
                var signedHash = await onionServiceClient.SignHashAsync(combinedHash);

                var signed = new BlockGraphProto
                {
                    Block = new BlockIDProto
                    {
                        Hash = message.BlockGraph.Block.SignedBlock.Coin.Stamp,
                        Node = message.Node,
                        Round = message.Round,
                        SignedBlock = new BlockProto
                        {
                            Key = message.BlockGraph.Block.SignedBlock.Coin.Stamp,
                            Coin = message.BlockGraph.Block.SignedBlock.Coin,
                            PublicKey = signedHash.PublicKey.ToHex(),
                            Signature = signedHash.Signature.ToHex()
                        }
                    }
                };

                return signed;
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningProvider.Sign >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<Core.API.Models.SignedHashResponse> Sign(SignedHashMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            return await onionServiceClient.SignHashAsync(message.Hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private byte[] BlockHash(SignedBlockHashMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrEmpty(message.Stamp))
                throw new ArgumentNullException(nameof(message.Stamp));

            if (message.Node <= 0)
                throw new ArgumentOutOfRangeException(nameof(message.Node));

            if (message.Round <= 0)
                throw new ArgumentOutOfRangeException(nameof(message.Round));

            if (message.PublicKey == null)
                throw new ArgumentNullException(nameof(message.PublicKey));

            if (message.PublicKey.Length < 32)
                throw new ArgumentOutOfRangeException(nameof(message.PublicKey));

            byte[] hash = null;

            try
            {
                hash = Cryptography.GenericHashWithKey($"{message.Stamp}{message.Node}{message.Round}", message.PublicKey);
            }
            catch (Exception ex)
            {
                logger.Warning($"<<< SigningProvider.BlockHash >>>: {ex.ToString()}");
            }

            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private byte[] HashCoin(SignedHashCoinMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Coin == null)
                throw new ArgumentNullException(nameof(message.Coin));

            byte[] hash = null;

            try
            {
                var serialized = Util.SerializeProto(message.Coin);

                hash = message.Key == null ? Cryptography.GenericHashNoKey(serialized) : Cryptography.GenericHashWithKey(serialized, message.Key);
            }
            catch (Exception ex)
            {
                logger.Warning($"<<< SigningProvider.HashCoin >>>: {ex.ToString()}");
            }

            return hash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool ValidateCoinRule(ValidateCoinRuleMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Coin == null)
                throw new ArgumentNullException(nameof(message.Coin));

            var coinHasElements = message.Coin.Validate().Any();
            if (!coinHasElements)
            {
                try
                {
                    using var secp256k1 = new Secp256k1();
                    using var bulletProof = new BulletProof();

                    var success = bulletProof.Verify(message.Coin.Commitment.FromHex(), message.Coin.RangeProof.FromHex(), null);
                    if (!success)
                        return false;
                }
                catch (Exception ex)
                {
                    logger.Error($"<<< SigningProvider.ValidateRule >>>: {ex.ToString()}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool VerifiyBlockSignature(VerifiyBlockSignatureMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.BlockID == null)
                throw new ArgumentNullException(nameof(message.BlockID));

            bool result = false;

            try
            {
                var signedBlock = message.BlockID.SignedBlock;
                var blockHash = BlockHash(new SignedBlockHashMessage(signedBlock.Coin.Stamp, message.BlockID.Node, message.BlockID.Round, signedBlock.PublicKey.FromHex()));
                var coinHash = HashCoin(new SignedHashCoinMessage(signedBlock.Coin, signedBlock.PublicKey.FromHex()));
                var combinedHash = Util.Combine(blockHash, coinHash);

                result = Ed25519.Verify(signedBlock.Signature.FromHex(), combinedHash, signedBlock.PublicKey.FromHex());
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningProvider.VerifiySignature >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool VerifiySignature(VerifiySignatureMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Signature == null)
                throw new ArgumentNullException(nameof(message.Signature));

            if (message.Message == null)
                throw new ArgumentNullException(nameof(message.Message));

            if (message.PublicKey == null)
                throw new ArgumentNullException(nameof(message.PublicKey));

            if (message.PublicKey.Length < 32)
                throw new ArgumentOutOfRangeException(nameof(message.PublicKey));

            bool result = false;

            try
            {
                result = Ed25519.Verify(message.Signature, message.Message, message.PublicKey);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningProvider.VerifiySignature >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool VerifiyHashChain(VerifiyHashChainMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Previous == null)
                throw new ArgumentNullException(nameof(message.Previous));

            if (message.Next == null)
                throw new ArgumentNullException(nameof(message.Next));

            bool validH = false, validK = false;

            try
            {
                var hint = Cryptography.GenericHashNoKey($"{message.Next.Version} {message.Next.Stamp} {message.Next.Principle}").ToHex();
                var keeper = Cryptography.GenericHashNoKey($"{message.Next.Version} {message.Next.Stamp} {message.Next.Hint}").ToHex();

                validH = message.Previous.Hint.Equals(hint);
                validK = message.Previous.Keeper.Equals(keeper);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningProvider.VerifiyHashChain >>>: {ex.ToString()}");
            }

            return validH && validK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onionServiceClient"></param>
        /// <returns></returns>
        public static Props Props(IOnionServiceClient onionServiceClient) =>
            Akka.Actor.Props.Create(() => new SigningActor(onionServiceClient));
    }
}
