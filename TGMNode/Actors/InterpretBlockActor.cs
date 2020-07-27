// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Secp256k1ZKP.Net;
using TGMCore.Actors;
using TGMCore.Providers;
using TGMCore.Extentions;
using TGMCore.Messages;
using TGMCore.Model;
using TGMNode.Model;

namespace TGMNode.Actors
{
    public class InterpretBlockActor : InterpretActor<TransactionProto>
    {
        private readonly IBaseBlockIDRepository<TransactionProto> _baseBlockIDRepository;

        public InterpretBlockActor(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider)
            : base(unitOfWork, signingActorProvider)
        {
            _baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<TransactionProto>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override async Task<bool> Interpret(InterpretMessage<TransactionProto> message)
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
                var txExists = await _baseBlockIDRepository
                    .GetFirstOrDefault(x => x.SignedBlock.Attach.Vin.K == block.SignedBlock.Attach.Vin.K && x.SignedBlock.Attach.Version == block.SignedBlock.Attach.Version);

                if (txExists != null)
                {
                    logger.Warning($"<<< InterpretBlockActor.Interpret >>>: Transaction exists for block {block.Round} from node {block.Node}");
                    continue;
                }

                var blockIdProto = new BaseBlockIDProto<TransactionProto> { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage<TransactionProto>(blockIdProto)))
                {
                    logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to verify signature for block {block.Round} from node {block.Node}");
                    continue;
                }

                if (!ValidateCoinRule(blockIdProto.SignedBlock.Attach))
                {
                    logger.Error($"<<< InterpretBlockActor.Interpret >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                    continue;
                }

                var txs = await _baseBlockIDRepository
                    .GetWhere(x => x.SignedBlock.Attach.Vin.K == blockIdProto.SignedBlock.Attach.Vin.K && x.Node == message.Node);

                if (txs?.Any() == true)
                {
                    var list = txs.ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        TransactionProto previous;
                        TransactionProto next;

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
                    }

                    using var pedersen = new Pedersen();

                    var sum = txs.Select(x => x.SignedBlock.Attach.Vout.C);
                    var success = pedersen.VerifyCommitSum(new List<byte[]> { sum.First().HexToBinary() }, sum.Select(x => x.HexToBinary()).Skip(1));
                    if (!success)
                    {
                        logger.Error($"<<< InterpretBlockActor.Interpret >>>: Could not verify committed sum for Interpreted BlockID");
                        return false;
                    }
                }

                var blockId = await _baseBlockIDRepository.StoreOrUpdate(blockIdProto);
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
        /// <param name="coin"></param>
        /// <returns></returns>
        private bool ValidateCoinRule(TransactionProto tx)
        {
            if (tx == null)
                throw new ArgumentNullException(nameof(tx));

            var txHasElements = tx.Validate().Any();
            if (!txHasElements)
            {
                try
                {
                    using var secp256k1 = new Secp256k1();
                    using var bulletProof = new BulletProof();

                    var success = bulletProof.Verify(tx.Vout.C[0].FromHex(), tx.Vout.R[0].FromHex(), null);
                    if (!success)
                        return false;
                }
                catch (Exception ex)
                {
                    logger.Error($"<<< SigningActor.ValidateRule >>>: {ex}");
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