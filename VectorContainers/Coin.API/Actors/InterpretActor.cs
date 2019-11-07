using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.ActorProviders;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using Secp256k1_ZKP.Net;

namespace Coin.API.Actors
{
    public class InterpretActor : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly ISigningActorProvider signingActorProvider;
        private readonly ILoggingAdapter logger;

        public InterpretActor(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            ReceiveAsync<InterpretBlocksMessage>(async msg => Sender.Tell(await Interpret(msg)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<bool> Interpret(InterpretBlocksMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (unitOfWork == null)
                throw new NullReferenceException(nameof(unitOfWork));

            if (signingActorProvider == null)
                throw new NullReferenceException(nameof(signingActorProvider));

            if (logger == null)
                throw new NullReferenceException(nameof(logger));

            foreach (var block in message.BlockIDs)
            {
                var coinExists = await unitOfWork.BlockID.HasCoin(block.SignedBlock.Coin.Commitment);
                if (coinExists)
                {
                    logger.Warning($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Coin exists for block {block.Round} from node {block.Node}");
                    continue;
                }
                
                var blockIdProto = new BlockIDProto { Hash = block.Hash, Node = block.Node, Round = block.Round, SignedBlock = block.SignedBlock };
                if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage(blockIdProto)))
                {
                    logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to verify signature for block {block.Round} from node {block.Node}");
                    continue;
                }

                if (!await signingActorProvider.ValidateCoinRule(new ValidateCoinRuleMessage(blockIdProto.SignedBlock.Coin)))
                {
                    logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                    continue;
                }

                var coins = await unitOfWork.BlockID
                    .GetWhere(x => x.SignedBlock.Coin.Stamp.Equals(blockIdProto.SignedBlock.Coin.Stamp) && x.Node.Equals(message.Node));

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

                        if (!await signingActorProvider.ValidateCoinRule(new ValidateCoinRuleMessage(previous)))
                        {
                            logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                            return false;
                        }

                        try
                        {
                            next = list[(i + 1) % (list.Count - 1)].SignedBlock.Coin;

                            if (!await signingActorProvider.ValidateCoinRule(new ValidateCoinRuleMessage(next)))
                            {
                                logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: unable to validate coin rule for block {block.Round} from node {block.Node}");
                                return false;
                            }
                        }
                        catch (DivideByZeroException)
                        {
                            next = blockIdProto.SignedBlock.Coin;
                        }

                        if (!await signingActorProvider.VerifiyHashChain(new VerifiyHashChainMessage(previous, next)))
                        {
                            logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not verify hash chain for Interpreted BlockID");
                            return false;
                        }
                    }

                    using var pedersen = new Pedersen();

                    var sum = coins.Select(c => c.SignedBlock.Coin.Commitment.FromHex());
                    var success = pedersen.VerifyCommitSum(new List<byte[]> { sum.First() }, sum.Skip(1));
                    if (!success)
                    {
                        logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not verify committed sum for Interpreted BlockID");
                        return false;
                    }
                }

                var blockId = await unitOfWork.BlockID.StoreOrUpdate(blockIdProto);
                if (blockId == null)
                {
                    logger.Error($"<<< InterpretBlocksProvider.InterpretBlocks >>>: Could not save block for {blockIdProto.Node} and round {blockIdProto.Round}");
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
        public static Props Props(IUnitOfWork unitOfWork, ISigningActorProvider signingActorProvider) =>
            Akka.Actor.Props.Create(() => new InterpretActor(unitOfWork, signingActorProvider));
    }
}
