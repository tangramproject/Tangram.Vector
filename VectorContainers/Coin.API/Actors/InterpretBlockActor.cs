using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Coin.API.Model;
using Core.API.Actors;
using Core.API.Actors.Providers;
using Core.API.Extentions;
using Core.API.LibSodium;
using Core.API.Messages;
using Core.API.Model;
using Secp256k1_ZKP.Net;

namespace Coin.API.Actors
{
    public class InterpretBlockActor : InterpretActor<CoinProto>
    {
        private readonly IBaseBlockIDRepository<CoinProto> baseBlockIDRepository;

        public InterpretBlockActor(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider)
            : base(unitOfWork, signingActorProvider)
        {
            baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<CoinProto>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override async Task<bool> Interpret(InterpretMessage<CoinProto> message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (unitOfWork == null)
                throw new NullReferenceException(nameof(unitOfWork));

            if (signingActorProvider == null)
                throw new NullReferenceException(nameof(signingActorProvider));

            if (logger == null)
                throw new NullReferenceException(nameof(logger));

            foreach (var block in message.Models)
            {
                var coinExists = await baseBlockIDRepository
                    .GetFirstOrDefault(x => x.SignedBlock.Attach.Stamp.Equals(block.SignedBlock.Attach.Stamp) && x.SignedBlock.Attach.Version.Equals(block.SignedBlock.Attach.Version));

                if (coinExists != null)
                {
                    logger.Warning($"<<< InterpretBlockActor.Interpret >>>: Coin exists for block {block.Round} from node {block.Node}");
                    continue;
                }

                var blockIdProto = new BaseBlockIDProto<CoinProto> { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage<CoinProto>(blockIdProto)))
                {
                    logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to verify signature for block {block.Round} from node {block.Node}");
                    continue;
                }

                if (!ValidateCoinRule(blockIdProto.SignedBlock.Attach))
                {
                    logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                    continue;
                }

                var coins = await baseBlockIDRepository
                    .GetWhere(x => x.SignedBlock.Attach.Stamp.Equals(blockIdProto.SignedBlock.Attach.Stamp) && x.Node.Equals(message.Node));

                if (coins?.Any() == true)
                {
                    var list = coins.ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        CoinProto previous;
                        CoinProto next;

                        try
                        {
                            previous = list[(i - 1) % (list.Count - 1)].SignedBlock.Attach;
                        }
                        catch (DivideByZeroException)
                        {
                            previous = list[i].SignedBlock.Attach;
                        }

                        if (!ValidateCoinRule(previous))
                        {
                            logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                            return false;
                        }

                        try
                        {
                            next = list[(i + 1) % (list.Count - 1)].SignedBlock.Attach;

                            if (!ValidateCoinRule(next))
                            {
                                logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                return false;
                            }
                        }
                        catch (DivideByZeroException)
                        {
                            next = blockIdProto.SignedBlock.Attach;
                        }

                        if (!VerifiyHashChain(previous, next))
                        {
                            logger.Error($"<<< InterpretBlockActor.Interpret >>>: Could not verify hash chain for Interpreted BlockID");
                            return false;
                        }
                    }

                    using var pedersen = new Pedersen();

                    var sum = coins.Select(c => c.SignedBlock.Attach.Commitment.FromHex());
                    var success = pedersen.VerifyCommitSum(new List<byte[]> { sum.First() }, sum.Skip(1));
                    if (!success)
                    {
                        logger.Error($"<<< InterpretBlockActor.Interpret >>>: Could not verify committed sum for Interpreted BlockID");
                        return false;
                    }
                }

                var blockId = await baseBlockIDRepository.StoreOrUpdate(blockIdProto);
                if (blockId == null)
                {
                    logger.Error($"<<< InterpretBlockActor.Interpret >>>: Could not save block for {blockIdProto.Node} and round {blockIdProto.Round}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private bool VerifiyHashChain(CoinProto previous, CoinProto next)
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
                logger.Error($"<<< SigningActor.VerifiyHashChain >>>: {ex.ToString()}");
            }

            return validH && validK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        private bool ValidateCoinRule(CoinProto coin)
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
                    logger.Error($"<<< SigningActor.ValidateRule >>>: {ex.ToString()}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider) =>
                Props.Create(() => new InterpretBlockActor(unitOfWork, signingActorProvider));
    }
}