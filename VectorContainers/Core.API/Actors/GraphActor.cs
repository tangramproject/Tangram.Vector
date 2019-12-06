using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Actors.Providers;
using Core.API.Consensus;
using Core.API.Extentions;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;
using Util = Core.API.Helper.Util;

namespace Core.API.Actors
{
    public class GraphActor<TAttach> : ReceiveActor
    {
        private const int requiredNodeCount = 4;

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly IInterpretActorProvider<TAttach> interpretActorProvider;
        private readonly IProcessActorProvider<TAttach> processActorProvider;
        private readonly ISigningActorProvider signingActorProvider;
        private readonly int totalNodes;
        private readonly ILoggingAdapter logger;
        private readonly IBaseGraphRepository<TAttach> baseGraphRepository;
        private readonly IJobRepository<TAttach> jobRepository;
        private readonly IBaseBlockIDRepository<TAttach> baseBlockIDRepository;

        private Graph Graph;
        private Config Config;

        private LastInterpretedMessage<TAttach> lastInterpretedMessage;
        private IActorRef jobActor;
        private IActorRef atLeastOnceDeliveryActor;

        public byte[] Id { get; private set; }

        public GraphActor(IUnitOfWork unitOfWork, IHttpClientService httpClientService, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;
            this.interpretActorProvider = interpretActorProvider;
            this.processActorProvider = processActorProvider;
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            jobRepository = unitOfWork.CreateJobOf<TAttach>();
            baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<TAttach>();

            totalNodes = httpClientService.Members.Count + 1;
            if (totalNodes < requiredNodeCount)
            {
                logger.Warning($"<<< GraphActor >>>: Minimum number of nodes required (4). Total number of nodes ({totalNodes})");
            }

            ReceiveAsync<HashedMessage>(async message => await Register(message));
            ReceiveAsync<ProcessBlockMessage<TAttach>>(async message => await Process(message));
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
                Shutdown(message, $"<<< GraphActor.Register >>>: Received hash mismatch. Got: ({message.Hash}) Expected: ({Id})");
                return;
            }

            var lastInterpreted = await LastInterpreted(message);

            if (Graph == null)
            {
                jobActor = CreateJob(message);

                Config = new Config(lastInterpreted, new ulong[totalNodes], httpClientService.NodeIdentity, (ulong)totalNodes);
                Graph = new Graph(Config);

                Graph.BlockmaniaInterpreted += (sender, e) => BlockmaniaCallback(sender, e).SwallowException();
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

            var blockID = await baseBlockIDRepository.GetLast(x => x.Hash.Equals(message.Hash.ToHex()));

            lastInterpretedMessage = blockID switch
            {
                null => new LastInterpretedMessage<TAttach>(0, null),
                _ => new LastInterpretedMessage<TAttach>(blockID.Round, blockID),
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

            Sender.Tell(new GracefulStopMessge(message.Hash, new TimeSpan(1), reason));
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

            var blockGraphs = await baseGraphRepository.GetWhere(x => x.Block.Hash.Equals(message.Hash.ToHex()) && !x.Included && !x.Replied);
            foreach (var blockGraph in blockGraphs)
            {
                var isSet = await SetOwnBlockGraph(blockGraph);
                if (isSet == null)
                {
                    logger.Warning($"<<< GraphActor.InitializeBlocks >>>: " +
                        $"Unable to set own block Hash: {blockGraph.Block.Hash} Round: {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                    continue;
                }

                if (string.IsNullOrEmpty(blockGraph.Block.SignedBlock.PublicKey) || string.IsNullOrEmpty(blockGraph.Block.SignedBlock.Signature))
                {
                    var success = await baseGraphRepository.Delete(blockGraph.Id);
                    if (!success)
                    {
                        logger.Error($"<<< GraphActor.InitializeBlocks >>>: Failed to delete block {blockGraph.Block.Hash}");
                        continue;
                    }
                }

                JobDelivery(message);

                await Process(new ProcessBlockMessage<TAttach>(isSet));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void JobDelivery(HashedMessage message)
        {
            var name = $"delivery-actor-{Util.HashToId(message.Hash.ToHex())}";
            var atLeastOnceProps = AtLeastOnceDeliveryActor.Create(jobActor, message.Hash.ToHex());

            atLeastOnceDeliveryActor = Context.ActorOf(atLeastOnceProps, name);
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
                var jobActorProps = JobActor<TAttach>.Create(unitOfWork, httpClientService);

                actorRef = Context.ActorOf(jobActorProps, name);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< GraphActor.CreateJob >>>: {ex.ToString()}");
            }

            return actorRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ulong GetLatestRound()
        {
            ulong result = 0;

            switch (lastInterpretedMessage.BlockID)
            {
                case null:
                    result += 1;
                    break;
                default:
                    result = lastInterpretedMessage.BlockID.Round;
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

            var interpretedList = new List<BaseBlockIDProto<TAttach>>();

            try
            {
                foreach (var block in interpreted.Blocks)
                {
                    var blockGraphs = await baseGraphRepository.GetWhere(x => x.Block.Hash.Equals(block.Hash) && x.Block.Node.Equals(httpClientService.NodeIdentity));
                    if (blockGraphs.Any() != true)
                    {
                        logger.Warning($"<<< GraphActor.BlockmaniaCallback >>>: Unable to find blocks with - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                        continue;
                    }

                    var blockGraph = blockGraphs.FirstOrDefault(x => x.Block.Node.Equals(httpClientService.NodeIdentity) && x.Block.Round.Equals(block.Round));
                    if (blockGraph == null)
                    {
                        logger.Error($"<<< GraphActor.BlockmaniaCallback >>>: Unable to find matching block - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                        continue;
                    }

                    interpretedList.Add(new BaseBlockIDProto<TAttach>
                    {
                        Hash = blockGraph.Block.Hash,
                        Node = blockGraph.Block.Node,
                        Round = blockGraph.Block.Round,
                        SignedBlock = blockGraph.Block.SignedBlock.Cast<BaseBlockProto<TAttach>>()
                    });
                }

                // Should return success blocks instead of bool.
                var success = await interpretActorProvider.Interpret(new InterpretMessage<TAttach>(httpClientService.NodeIdentity, interpretedList));
                if (success)
                {
                    await jobRepository.SetStates(interpretedList.Select(x => x.Hash), JobState.Polished);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< GraphActor.BlockmaniaCallback >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task Process(ProcessBlockMessage<TAttach> message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.BlockGraph == null)
                throw new ArgumentNullException(nameof(message.BlockGraph));

            var job = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(Id.ToHex()) && x.Status == JobState.Blockmainia);
            if (job != null)
            {
                var processed = await processActorProvider.Process(new BlockGraphMessage<TAttach>(message.BlockGraph));
                if (processed.Equals(false))
                {
                    return;
                }

                Graph.Add(message.BlockGraph.ToBlockGraph());

                await jobRepository.SetState(job, JobState.Running);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BaseGraphProto<TAttach>> SetOwnBlockGraph(BaseGraphProto<TAttach> blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            try
            {
                bool copy = false;
                ulong round = 0;
                ulong node = 0;

                copy |= !blockGraph.Block.Node.Equals(httpClientService.NodeIdentity);

                if (copy)
                {
                    node = blockGraph.Block.Node;
                    round = blockGraph.Block.Round;
                }

                if (!copy)
                {
                    round = await IncrementRound(blockGraph.Block.Hash);
                    node = httpClientService.NodeIdentity;
                }

                var signed = await Sign(node, round, blockGraph);
                var prev = await baseGraphRepository.GetPrevious(signed.Block.Hash, node, round);

                if (prev != null)
                {
                    if (prev.Block.Round + 1 != signed.Block.Round)
                        signed.Prev = prev.Block;
                }

                round = GetLatestRound();

                if (round + 1 < signed.Block.Round)
                {
                    return null;
                }

                var stored = await SetBlockGraph(signed);
                if (stored != null)
                {
                    return stored;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< GraphActor.SetOwnBlockGraph >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="round"></param>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BaseGraphProto<TAttach>> Sign(ulong node, ulong round, BaseGraphProto<TAttach> blockGraph)
        {
            var hash = Util.SerializeProto(blockGraph);
            var signedHashResponse = await signingActorProvider.Sign(new SignedHashMessage(hash));
            var signed = new BaseGraphProto<TAttach>
            {
                Block = new BaseBlockIDProto<TAttach>
                {
                    Hash = Id.ToHex(),
                    Node = node,
                    Round = round,
                    SignedBlock = new BaseBlockProto<TAttach>
                    {
                        Key = Id.ToHex(),
                        Attach = blockGraph.Block.SignedBlock.Attach,
                        PublicKey = signedHashResponse.PublicKey.ToHex(),
                        Signature = signedHashResponse.Signature.ToHex()
                    }
                }
            };

            return signed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BaseGraphProto<TAttach>> SetBlockGraph(BaseGraphProto<TAttach> blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            try
            {
                var can = await baseGraphRepository.CanAdd(blockGraph, blockGraph.Block.Node);
                if (can == null)
                {
                    return null;
                }

                var stored = await baseGraphRepository.StoreOrUpdate(new BaseGraphProto<TAttach>
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
                logger.Error($"<<< GraphActor.SetBlockGraph >>>: {ex.ToString()}");
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
                var blockIDs = await baseGraphRepository.GetWhere(x => x.Block.Hash.Equals(hash));
                if (blockIDs.Any())
                {
                    round = blockIDs.Last().Block.Round;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< GraphActor.IncrementRound >>>: {ex.ToString()}");
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
        /// <param name="processActorProvider"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IHttpClientService httpService, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider) =>
            Props.Create(() => new GraphActor<TAttach>(unitOfWork, httpService, interpretActorProvider, processActorProvider, signingActorProvider));
    }
}
