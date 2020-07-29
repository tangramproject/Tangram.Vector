// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Providers;
using TGMCore.Extensions;
using TGMCore.Extentions;
using TGMCore.Helper;
using TGMCore.Messages;
using TGMCore.Model;

namespace TGMCore.Actors
{
    public class JobActor<TAttach> : ReceiveActor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClusterProvider _clusterProvider;
        private readonly ILoggingAdapter _logger;
        private readonly IBaseGraphRepository<TAttach> _baseGraphRepository;
        private readonly IJobRepository<TAttach> _jobRepository;

        private readonly Dictionary<IActorRef, HashSet<long>> _ackBuffer;

        public byte[] Id { get; private set; }

        public JobActor(IUnitOfWork unitOfWork, IClusterProvider clusterProvider)
        {
            _unitOfWork = unitOfWork;
            _clusterProvider = clusterProvider;

            _logger = Context.GetLogger();

            _ackBuffer = new Dictionary<IActorRef, HashSet<long>>();

            _baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            _jobRepository = unitOfWork.CreateJobOf<TAttach>();

            Receive<ReliableDeliveryEnvelopeMessage<WriteMessage>>(
                write => _ackBuffer.ContainsKey(Sender) && _ackBuffer[Sender].Contains(write.MessageId),
                write =>
            {
                Sender.Tell(new ReliableDeliveryAckMessage(write.MessageId));
            });


            ReceiveAsync<ReliableDeliveryEnvelopeMessage<WriteMessage>>(async write =>
            {
                Sender.Tell(new ReliableDeliveryAckMessage(write.MessageId));

                if (!_ackBuffer.ContainsKey(Sender))
                {
                    _ackBuffer.Add(Sender, new HashSet<long>());
                }

                _ackBuffer[Sender].Add(write.MessageId);

                await Register(new HashedMessage(write.Message.Content.FromHex()));
                await Sender.GracefulStop(TimeSpan.FromSeconds(5));
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Register(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            try
            {
                Id ??= message.Hash;

                if (!Id.SequenceEqual(message.Hash))
                {
                    Shutdown(message, $"<<< JobActor.Register >>>: Received hash mismatch. Got: ({message.Hash}) Expected: ({Id})");
                    return;
                }

                var blocks = await _baseGraphRepository.GetWhere(x => x.Block.Hash.Equals(message.Hash.ToHex()) && !x.Included);
                if (blocks.Any())
                {
                    var moreBlocks = await _baseGraphRepository.More(blocks);
                    var blockHashLookup = moreBlocks.ToLookup(i => i.Block.Hash);

                    await _baseGraphRepository.Include(blocks, _clusterProvider.GetSelfUniqueAddress());
                    await AddOrUpdateJob(blockHashLookup);
                }

                Context.Stop(Self);
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< JobActor.Register >>>: {ex}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="reason"></param>
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

            _logger.Warning(reason);

            Context.Stop(Self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        private async Task<JobProto<TAttach>> AddJob(BaseGraphProto<TAttach> next)
        {
            if (next.IsDefault())
                throw new ArgumentNullException(nameof(next));

            JobProto<TAttach> job = null;

            try
            {
                job = new JobProto<TAttach>
                {
                    Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Hash = next.Block.Hash,
                    Model = next,
                    ExpectedTotalNodes = 4,
                    Node = _clusterProvider.GetSelfUniqueAddress(),
                    TotalNodes = _clusterProvider.GetMembers().Count(),
                    Status = JobState.Started
                };

                job.Nodes = new List<ulong>();
                job.Nodes.AddRange(next.Deps?.Select(n => n.Block.Node));

                job.WaitingOn = new List<ulong>();
                job.WaitingOn.AddRange(_clusterProvider.GetMembers().Select(k => (ulong)k.UniqueAddress.Uid).ToArray());

                job.Status = Incoming(job, next);

                ClearWaitingOn(job);

                await _jobRepository.StoreOrUpdate(job);
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< JobActor.AddJob >>>: {ex}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        private static void ClearWaitingOn(JobProto<TAttach> job)
        {
            if (job.Status == JobState.Blockmainia || job.Status == JobState.Running)
            {
                job.WaitingOn.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        private async Task<JobProto<TAttach>> ExistingJob(BaseGraphProto<TAttach> next)
        {
            if (next.IsDefault())
                throw new ArgumentNullException(nameof(next));

            JobProto<TAttach> job = null;

            try
            {
                var jobProto = await _jobRepository.GetFirstOrDefault(x => x.Hash == next.Block.Hash);

                if (jobProto != null)
                {
                    jobProto.Nodes = new List<ulong>();
                    jobProto.Nodes.AddRange(next.Deps?.Select(n => n.Block.Node));

                    jobProto.WaitingOn = new List<ulong>();
                    jobProto.WaitingOn.AddRange(_clusterProvider.GetMembers().Select(k => (ulong)k.UniqueAddress.Uid).ToArray());

                    if (!jobProto.Model.Equals(next))
                    {
                        jobProto.Status = Incoming(jobProto, next);

                        ClearWaitingOn(jobProto);
                    }

                    jobProto.Model = next;

                    await _jobRepository.Include(jobProto);

                    job = await _jobRepository.StoreOrUpdate(jobProto, jobProto.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< JobActor.ExistingJob >>>: {ex}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="job"></param>
        public static JobState Incoming(JobProto<TAttach> job, BaseGraphProto<TAttach> next)
        {
            if (job.Nodes.Any())
            {

                var nodes = job.Nodes?.Except(next.Deps.Select(x => x.Block.Node));
                if (nodes.Any() != true)
                {
                    return JobState.Blockmainia;
                }
            }

            if (job.WaitingOn.Any())
            {
                var waitingOn = job.WaitingOn?.Except(next.Deps.Select(x => x.Block.Node));
                if (waitingOn.Any() != true)
                {
                    return JobState.Blockmainia;
                }
            }

            return job.Status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockHashLookup"></param>
        /// <returns></returns>
        private async Task AddOrUpdateJob(ILookup<string, BaseGraphProto<TAttach>> blockHashLookup)
        {
            if (blockHashLookup.Any() != true)
            {
                return;
            }

            foreach (var next in BaseGraphProto<TAttach>.NextBlockGraph(blockHashLookup, _clusterProvider.GetSelfUniqueAddress()))
            {
                var jobProto = await _jobRepository.GetFirstOrDefault(x => x.Hash == next.Block.Hash);
                if (jobProto != null)
                {
                    if (jobProto.Status != JobState.Blockmainia &&
                        jobProto.Status != JobState.Running &&
                        jobProto.Status != JobState.Polished)
                    {
                        await ExistingJob(next);
                    }

                    continue;
                }

                await AddJob(next);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="clusterProvider"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IClusterProvider clusterProvider) =>
            Props.Create(() => new JobActor<TAttach>(unitOfWork, clusterProvider));

    }
}
