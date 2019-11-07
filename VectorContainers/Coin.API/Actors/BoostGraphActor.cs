using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.ActorProviders;
using Coin.API.Services;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;

namespace Coin.API.Actors
{
    public class BoostGraphActor : ReceiveActor
    {
        private const int requiredNodeCount = 4;

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly IInterpretActorProvider interpretActorProvider;
        private readonly IProcessBlockActorProvider processBlockActorProvider;
        private readonly ISigningActorProvider signingActorProvider;
        private readonly int totalNodes;
        private readonly ILoggingAdapter logger;

        private Graph Graph;
        private Config Config;

        private LastInterpretedMessage lastInterpretedMessage;
        private byte[] publicKey;
        private IActorRef jobActor;

        public byte[] Id { get; private set; }

        public BoostGraphActor(IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
            IProcessBlockActorProvider processBlockActorProvider, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.interpretActorProvider = interpretActorProvider;
            this.processBlockActorProvider = processBlockActorProvider;
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            totalNodes = httpService.Members.Count + 1;
            if (totalNodes < requiredNodeCount)
            {
                logger.Warning($"<<< BoostGraph >>>: Minimum number of nodes required (4). Total number of nodes ({totalNodes})");
            }

            ReceiveAsync<HashedMessage>(async message => await Register(message));
            ReceiveAsync<ProcessBlockMessage>(async message => await Process(message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task Register(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            Id ??= message.Hash;

            if (!Id.SequenceEqual(message.Hash))
            {
                Shutdown(message, $"<<< BoostGraphActor.Register >>>: Received hash mismatch. Got: ({message.Hash}) Expected: ({Id})");
                return;
            }

            var lastInterpreted = await LastInterpreted(message);

            if (Graph == null)
            {
                jobActor = CreateJob(message);

                Config = new Config(lastInterpreted, new ulong[totalNodes], httpService.NodeIdentity, (ulong)totalNodes);
                Graph = new Graph(Config);

                Graph.BlockmaniaInterpreted += (sender, e) => BlockmaniaCallback(sender, e).SwallowException();

                try
                {
                    publicKey = await httpService.GetPublicKey();
                }
                catch (Exception ex)
                {
                    logger.Error($"<<< BoostGraphActor.Register >>>: {ex.ToString()}");
                    Shutdown(new HashedMessage(Id), "Public key not found.");
                    return;
                }
            }

            await InitializeBlocks(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<ulong> LastInterpreted(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            var blockID = await unitOfWork.BlockID.GetFirstOrDefault(x => x.Hash.Equals(message.Hash.ToHex()));

            lastInterpretedMessage = blockID switch
            {
                null => new LastInterpretedMessage(0, null),
                _ => new LastInterpretedMessage(blockID.Round, blockID),
            };

            return lastInterpretedMessage.Last > 0 ? lastInterpretedMessage.Last - 1 : lastInterpretedMessage.Last;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void Shutdown(HashedMessage message, string reason)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));

            Context.ActorSelection("../sip-actor").Tell(new GracefulStopMessge(message.Hash, new TimeSpan(1), reason));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task InitializeBlocks(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            var blockGraphs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(message.Hash.ToHex()) && !x.Included && !x.Replied);
            foreach (var blockGraph in blockGraphs)
            {
                var isSet = await SetOwnBlockGraph(blockGraph);
                if (isSet == null)
                {
                    logger.Warning($"<<< BoostGraphActor.InitializeBlocks >>>: " +
                        $"Unable to set own block Hash: {blockGraph.Block.Hash} Round: {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                    continue;
                }

                if (string.IsNullOrEmpty(blockGraph.Block.SignedBlock.PublicKey) || string.IsNullOrEmpty(blockGraph.Block.SignedBlock.Signature))
                {
                    var success = await unitOfWork.BlockGraph.Delete(blockGraph.Id);
                    if (!success)
                    {
                        logger.Error($"<<< BoostGraphActor.InitializeBlocks >>>: Failed to delete block {blockGraph.Block.Hash}");
                        continue;
                    }
                }

                jobActor.Tell(new HashedMessage(isSet.Block.Hash.FromHex()));

                await Process(new ProcessBlockMessage(isSet));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private IActorRef CreateJob(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            IActorRef actorRef = null;

            try
            {
                var name = $"job-actor-{Util.HashToId(message.Hash.ToHex())}";
                actorRef = Context.ActorOf(JobActor.Props(unitOfWork, httpService), name);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< BoostGraphActor.CreateJob >>>: {ex.ToString()}");
            }

            return actorRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ulong GetVersion()
        {
            ulong result = 0;

            switch (lastInterpretedMessage.BlockID)
            {
                case null:
                    result += 1;
                    break;
                default:
                    result = (ulong)lastInterpretedMessage.BlockID.SignedBlock.Coin.Version + 1;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interpreted"></param>
        private async Task BlockmaniaCallback(object sender, Interpreted interpreted)
        {
            if (interpreted == null)
                throw new ArgumentNullException(nameof(interpreted));

            var interpretedList = new List<BlockID>();

            try
            {
                foreach (var block in interpreted.Blocks)
                {
                    var blockGraphs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(block.Hash) && x.Block.Node.Equals(httpService.NodeIdentity));
                    if (blockGraphs.Any() != true)
                    {
                        logger.Warning($"<<< BoostGraphActor.BlockmaniaCallback >>>: Unable to find blocks with - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                        continue;
                    }

                    var blockGraph = blockGraphs.FirstOrDefault(x => x.Block.Node.Equals(httpService.NodeIdentity) && x.Block.Round.Equals(block.Round));
                    if (blockGraph == null)
                    {
                        logger.Error($"<<< BoostGraphActor.BlockmaniaCallback >>>: Unable to find matching block - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                        continue;
                    }

                    interpretedList.Add(new BlockID(blockGraph.Block.Hash, blockGraph.Block.Node, blockGraph.Block.Round, blockGraph.Block.SignedBlock));
                }

                // Should return success blocks instead of bool.
                var success = await interpretActorProvider.Interpret(new InterpretBlocksMessage(httpService.NodeIdentity, interpretedList));
                if (success)
                {
                    await unitOfWork.Job.SetStates(interpretedList.Select(x => x.Hash), JobState.Polished);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< BoostGraphActor.BlockmaniaCallback >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task Process(ProcessBlockMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.BlockGraph == null)
                throw new ArgumentNullException(nameof(message.BlockGraph));

            var job = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(Id.ToHex()) && x.Status == JobState.Blockmainia);
            if (job != null)
            {
                var block = await processBlockActorProvider.ProcessBlock(new BlockGraphMessage(message.BlockGraph));
                if (block == null)
                {
                    return;
                }

                Graph.Add(block.ToBlockGraph());

                await unitOfWork.Job.SetState(job, JobState.Running);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> SetOwnBlockGraph(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            try
            {
                bool copy = false;
                ulong round = 0;
                ulong node = 0;

                copy |= !blockGraph.Block.Node.Equals(httpService.NodeIdentity);

                if (copy)
                {
                    node = blockGraph.Block.Node;
                    round = blockGraph.Block.Round;
                }

                if (!copy)
                {
                    round = await IncrementRound(blockGraph.Block.Hash);
                    node = httpService.NodeIdentity;
                }

                var signed = await signingActorProvider.Sign(new SignedBlockGraphMessage(node, blockGraph, round, publicKey));
                var prev = await unitOfWork.BlockGraph.GetPrevious(signed.Block.Hash, node, round);

                if (prev != null)
                {
                    if (prev.Block.Round + 1 != signed.Block.Round)
                        signed.Prev = prev.Block;
                }

                var stored = await SetBlockGraph(signed);
                if (stored != null)
                {
                    return stored;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< BoostGraphActor.SetOwnBlockGraph >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> SetBlockGraph(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            try
            {
                var can = await unitOfWork.BlockGraph.CanAdd(blockGraph, blockGraph.Block.Node);
                if (can == null)
                {
                    return null;
                }

                var stored = await unitOfWork.BlockGraph.StoreOrUpdate(new BlockGraphProto
                {
                    Block = blockGraph.Block,
                    Deps = blockGraph.Deps?.Select(d => d).ToList(),
                    Prev = blockGraph.Prev ?? null,
                    Included = blockGraph.Included,
                    Replied = blockGraph.Replied
                });

                return stored;

            }
            catch (Exception ex)
            {
                logger.Error($"<<< BlockGraphService.SetBlockGraph >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private async Task<ulong> IncrementRound(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash));

            ulong round = 0;

            try
            {
                var blockIDs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(hash));
                if (blockIDs.Any())
                {
                    round = blockIDs.Last().Block.Round;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< BlockGraphService.IncrementRound >>>: {ex.ToString()}");
            }

            round += 1;

            return round;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="httpService"></param>
        /// <param name="interpretActorProvider"></param>
        /// <param name="processBlockActorProvider"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Props(IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
            IProcessBlockActorProvider processBlockActorProvider, ISigningActorProvider signingActorProvider) =>
            Akka.Actor.Props.Create(() => new BoostGraphActor(unitOfWork, httpService, interpretActorProvider, processBlockActorProvider, signingActorProvider));
    }
}
