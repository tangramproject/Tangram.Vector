using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Actors.Providers;
using Core.API.Messages;

namespace Core.API.Actors
{
    public class ProcessActor<TAttach> : ReceiveActor
    {
        private readonly ISigningActorProvider signingActorProvider;
        private readonly ILoggingAdapter logger;

        public ProcessActor(ISigningActorProvider signingActorProvider)
        {
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            ReceiveAsync<BlockGraphMessage<TAttach>>(async message => Sender.Tell(await Process(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<bool> Process(BlockGraphMessage<TAttach> message)
        {
            try
            {
                if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage<TAttach>(message.BaseGraph.Block)))
                {
                    logger.Error($"<<< ProcessActor.Process >>>: Unable to verify signature for block {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                    return false;
                }

                if (message.BaseGraph.Prev != null && message.BaseGraph.Prev?.Round != 0)
                {
                    if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage<TAttach>(message.BaseGraph.Prev)))
                    {
                        logger.Error($"<<< ProcessActor.Process >>>: Unable to verify signature for previous block on block {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                        return false;
                    }

                    if (message.BaseGraph.Prev.Node != message.BaseGraph.Block.Node)
                    {
                        logger.Error($"<<< ProcessActor.Process >>>: Previous block node does not match on block {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                        return false;
                    }

                    if (message.BaseGraph.Prev.Round + 1 != message.BaseGraph.Block.Round)
                    {
                        logger.Error($"<<< ProcessActor.Process >>>: Previous block round is invalid on block {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                        return false;
                    }
                }

                for (int i = 0; i < message.BaseGraph.Deps.Count(); i++)
                {
                    var dep = message.BaseGraph.Deps[i];

                    if (!await signingActorProvider.VerifiyBlockSignature(new VerifiyBlockSignatureMessage<TAttach>(dep.Block)))
                    {
                        logger.Error($"<<< ProcessActor.Process >>>: Unable to verify signature for block reference {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                        return false;
                    }

                    if (dep.Block.Node == message.BaseGraph.Block.Node)
                    {
                        logger.Error($"<<< ProcessActor.Process >>>: Block references includes a block from same node in block reference  {message.BaseGraph.Block.Round} from node {message.BaseGraph.Block.Node}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< ProcessActor.Process >>>: {ex.ToString()}");
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Create(ISigningActorProvider signingActorProvider) =>
            Props.Create(() => new ProcessActor<TAttach>(signingActorProvider));
    }
}
