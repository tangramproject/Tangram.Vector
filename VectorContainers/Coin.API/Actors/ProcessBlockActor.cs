using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.ActorProviders;
using Core.API.Messages;
using Core.API.Model;

namespace Coin.API.Actors
{
    public class ProcessBlockActor : ReceiveActor
    {
        private readonly ISigningActorProvider signingActorProvider;
        private readonly ILoggingAdapter logger;

        public ProcessBlockActor(ISigningActorProvider signingActorProvider)
        {
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            ReceiveAsync<BlockGraphMessage>(async message => Sender.Tell(await ProcessBlock(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> ProcessBlock(BlockGraphMessage message)
        {
            try
            {
                if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage(message.BlockGraph.Block)))
                {
                    logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Unable to verify signature for block {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                    return null;
                }

                if (message.BlockGraph.Prev != null && message.BlockGraph.Prev?.Round != 0)
                {
                    if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage(message.BlockGraph.Prev)))
                    {
                        logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Unable to verify signature for previous block on block {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                        return null;
                    }

                    if (message.BlockGraph.Prev.Node != message.BlockGraph.Block.Node)
                    {
                        logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Previous block node does not match on block {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                        return null;
                    }

                    if (message.BlockGraph.Prev.Round + 1 != message.BlockGraph.Block.Round)
                    {
                        logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Previous block round is invalid on block {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                        return null;
                    }
                }

                for (int i = 0; i < message.BlockGraph.Deps.Count(); i++)
                {
                    var dep = message.BlockGraph.Deps[i];

                    if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage(dep.Block)))
                    {
                        logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Unable to verify signature for block reference {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                        return null;
                    }

                    if (dep.Block.Node == message.BlockGraph.Block.Node)
                    {
                        logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: Block references includes a block from same node in block reference  {message.BlockGraph.Block.Round} from node {message.BlockGraph.Block.Node}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< ProcessBlockProvider.ProcessBlocks >>>: {ex.ToString()}");
            }

            return message.BlockGraph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Props(ISigningActorProvider signingActorProvider) =>
            Akka.Actor.Props.Create(() => new ProcessBlockActor(signingActorProvider));
    }
}
