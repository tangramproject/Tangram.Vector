// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Providers;
using TGMCore.Consensus;
using TGMCore.Extentions;
using TGMCore.Messages;
using TGMCore.Model;
using Util = TGMCore.Helper.Util;

namespace TGMCore.Actors
{
    public class GraphActor<TAttach> : ReceiveActor
    {
        private const string _keyPurpose = "GraphActor.Key";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IClusterProvider _clusterProvider;
        private readonly IInterpretActorProvider<TAttach> _interpretActorProvider;
        private readonly IProcessActorProvider<TAttach> _processActorProvider;
        private readonly ISigningActorProvider _signingActorProvider;
        private readonly IPubProvider _pubProvider;
        private readonly int _totalNodes;
        private readonly ILoggingAdapter _logger;
        private readonly IBaseGraphRepository<TAttach> _baseGraphRepository;
        private readonly IJobRepository<TAttach> _jobRepository;
        private readonly IBaseBlockIDRepository<TAttach> _baseBlockIDRepository;
        
        private Graph Graph;
        private Config Config;

        private LastInterpretedMessage<TAttach> _lastInterpretedMessage;
        private IActorRef _jobActor;
        private IActorRef _atLeastOnceDeliveryActor;

        public byte[] Id { get; private set; }

        public GraphActor(IUnitOfWork unitOfWork,
            IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider,
            IPubProvider pubProvider)
        {
            _unitOfWork = unitOfWork;
            _clusterProvider = clusterProvider;
            _interpretActorProvider = interpretActorProvider;
            _processActorProvider = processActorProvider;
            _signingActorProvider = signingActorProvider;
            _pubProvider = pubProvider;

            _logger = Context.GetLogger();

            _baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            _jobRepository = unitOfWork.CreateJobOf<TAttach>();
            _baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<TAttach>();

            _totalNodes = _clusterProvider.GetMembers().Count();
            if (_totalNodes < _clusterProvider.GetInitialQuorumSize())
            {
                _logger.Warning($"<<< GraphActor >>>: Minimum number of nodes required (4). Total number of nodes ({_totalNodes})");
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

            if (message.Hash.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            Id ??= message.Hash;

            if (!Id.SequenceEqual(message.Hash))
            {
                Shutdown(message, $"<<< GraphActor.Register >>>: Received hash mismatch. Got: ({message.Hash}) Expected: ({Id})");
                return;
            }

            _ = await _signingActorProvider.CreateKeyPurpose(new KeyPurposeMessage(_keyPurpose));
            var lastInterpreted = await LastInterpreted(message);

            if (Graph == null)
            {
                _jobActor = CreateJob(message);

                Config = new Config(lastInterpreted, new ulong[_totalNodes], _clusterProvider.GetSelfUniqueAddress(), (ulong)_totalNodes);
                Graph = new Graph(Config);

                Graph.BlockmaniaInterpreted += (sender, e) => BlockmaniaCallback(sender, e).SwallowException();
            }

            await InitializeBlocks(message);
            await _pubProvider.PublishAsync(ChatMessage.Empty());
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

            if (message.Hash.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            var blockID = await _baseBlockIDRepository.GetLast(x => x.Hash == message.Hash.ToHex());

            _lastInterpretedMessage = blockID switch
            {
                null => new LastInterpretedMessage<TAttach>(0, null),
                _ => new LastInterpretedMessage<TAttach>(blockID.Round, blockID),
            };

            return _lastInterpretedMessage.Last > 0 ? _lastInterpretedMessage.Last - 1 : _lastInterpretedMessage.Last;
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

            if (message.Hash.Length != 33)
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

            if (message.Hash.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            var blockGraphs = await _baseGraphRepository.GetWhere(x => x.Block.Hash == message.Hash.ToHex() && !x.Included && !x.Replied);
            foreach (var blockGraph in blockGraphs)
            {
                var isSet = await SetOwnBlockGraph(blockGraph);
                if (isSet == null)
                {
                    _logger.Warning($"<<< GraphActor.InitializeBlocks >>>: " +
                        $"Unable to set own block Hash: {blockGraph.Block.Hash} Round: {blockGraph.Block.Round} from node {blockGraph.Block.Node}");
                    continue;
                }

                if (string.IsNullOrEmpty(blockGraph.Block.SignedBlock.PublicKey) || string.IsNullOrEmpty(blockGraph.Block.SignedBlock.Signature))
                {
                    var success = await _baseGraphRepository.Delete(blockGraph.Id);
                    if (!success)
                    {
                        _logger.Error($"<<< GraphActor.InitializeBlocks >>>: Failed to delete block {blockGraph.Block.Hash}");
                        continue;
                    }
                }

                _jobActor.Tell(message);

                await Process(new ProcessBlockMessage<TAttach>(isSet));
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

            if (message.Hash.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            IActorRef actorRef = null;

            try
            {
                var name = $"job-actor-{Util.HashToId(message.Hash.ToHex())}";
                var jobActorProps = JobActor<TAttach>.Create(_unitOfWork, _clusterProvider);

                actorRef = Context.System.ActorOf(jobActorProps, name);
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< GraphActor.CreateJob >>>: {ex}");
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

            switch (_lastInterpretedMessage.BlockID)
            {
                case null:
                    result += 1;
                    break;
                default:
                    result = _lastInterpretedMessage.BlockID.Round;
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
                    var blockGraphs = await _baseGraphRepository.GetWhere(x => x.Block.Hash == block.Hash && x.Block.Node == _clusterProvider.GetSelfUniqueAddress());
                    if (blockGraphs.Any() != true)
                    {
                        _logger.Warning($"<<< GraphActor.BlockmaniaCallback >>>: Unable to find blocks with - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
                        continue;
                    }

                    var blockGraph = blockGraphs.FirstOrDefault(x => x.Block.Node == _clusterProvider.GetSelfUniqueAddress() && x.Block.Round == block.Round);
                    if (blockGraph == null)
                    {
                        _logger.Error($"<<< GraphActor.BlockmaniaCallback >>>: Unable to find matching block - Hash: {block.Hash} Round: {block.Round} from node {block.Node}");
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
                var success = await _interpretActorProvider.Interpret(new InterpretMessage<TAttach>(_clusterProvider.GetSelfUniqueAddress(), interpretedList));
                if (success)
                {
                    await _jobRepository.SetStates(interpretedList.Select(x => x.Hash), JobState.Polished);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< GraphActor.BlockmaniaCallback >>>: {ex}");
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

            var job = await _jobRepository.GetFirstOrDefault(x => x.Hash == Id.ToHex() && x.Status == JobState.Blockmainia);
            if (job != null)
            {
                var processed = await _processActorProvider.Process(new BlockGraphMessage<TAttach>(message.BlockGraph));
                if (processed.Equals(false))
                {
                    return;
                }

                Graph.Add(message.BlockGraph.ToBlockGraph());

                await _jobRepository.SetState(job, JobState.Running);
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

                copy |= blockGraph.Block.Node != _clusterProvider.GetSelfUniqueAddress();

                if (copy)
                {
                    node = blockGraph.Block.Node;
                    round = blockGraph.Block.Round;
                }

                if (!copy)
                {
                    round = await IncrementRound(blockGraph.Block.Hash);
                    node = _clusterProvider.GetSelfUniqueAddress();
                }

                var signed = await Sign(node, round, blockGraph);
                var prev = await _baseGraphRepository.GetPrevious(signed.Block.Hash, node, round);

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
                _logger.Error($"<<< GraphActor.SetOwnBlockGraph >>>: {ex}");
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
            var signedHashResponse = await _signingActorProvider.Sign(new SignedHashMessage(hash, _keyPurpose));
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
                var can = await _baseGraphRepository.CanAdd(blockGraph, blockGraph.Block.Node);
                if (can == null)
                {
                    return null;
                }

                var stored = await _baseGraphRepository.StoreOrUpdate(new BaseGraphProto<TAttach>
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
                _logger.Error($"<<< GraphActor.SetBlockGraph >>>: {ex}");
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
                var blockIDs = await _baseGraphRepository.GetWhere(x => x.Block.Hash == hash);
                if (blockIDs.Any())
                {
                    round = blockIDs.Last().Block.Round;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< GraphActor.IncrementRound >>>: {ex}");
            }

            round += 1;

            return round;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="clusterProvider"></param>
        /// <param name="interpretActorProvider"></param>
        /// <param name="processActorProvider"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider, IPubProvider pubProvider) =>
            Props.Create(() => new GraphActor<TAttach>(unitOfWork, clusterProvider, interpretActorProvider, processActorProvider, signingActorProvider, pubProvider));
    }
}
