using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Extensions;
using Core.API.Extentions;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;

namespace Core.API.Actors
{
    public class JobActor<TAttach> : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly ILoggingAdapter logger;
        private readonly IBaseGraphRepository<TAttach> baseGraphRepository;
        private readonly IJobRepository<TAttach> jobRepository;

        public byte[] Id { get; private set; }

        public JobActor(IUnitOfWork unitOfWork, IHttpClientService httpClientService)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;

            logger = Context.GetLogger();

            baseGraphRepository = unitOfWork.CreateBaseGraphOf<TAttach>();
            jobRepository = unitOfWork.CreateJobOf<TAttach>();

            ReceiveAsync<ReliableDeliveryEnvelopeMessage<WriteMessage>>(async write =>
            {
                Sender.Tell(new ReliableDeliveryAckMessage(write.MessageId));

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

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            try
            {
                Id ??= message.Hash;

                if (!Id.SequenceEqual(message.Hash))
                {
                    Shutdown(message, $"<<< JobActor.Register >>>: Received hash mismatch. Got: ({message.Hash}) Expected: ({Id})");
                    return;
                }

                var blocks = await baseGraphRepository.GetWhere(x => x.Block.Hash.Equals(message.Hash.ToHex()) && !x.Included);
                if (blocks.Any())
                {
                    var moreBlocks = await baseGraphRepository.More(blocks);
                    var blockHashLookup = moreBlocks.ToLookup(i => i.Block.Hash);

                    await baseGraphRepository.Include(blocks, httpClientService.NodeIdentity);
                    await AddOrUpdateJob(blockHashLookup);
                }

                Context.Stop(Self);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< JobActor.Register >>>: {ex.ToString()}");
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

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));

            logger.Warning(reason);

            Context.Stop(Self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        private async Task<JobProto<TAttach>> AddJob(TAttach next)
        {
            if (next.IsDefault())
                throw new ArgumentNullException(nameof(next));

            JobProto<TAttach> job = null;

            try
            {
                var n = next as BaseGraphProto<TAttach>;

                job = new JobProto<TAttach>
                {
                    Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Hash = n.Block.Hash,
                    Model = n,
                    ExpectedTotalNodes = 4,
                    Node = httpClientService.NodeIdentity,
                    TotalNodes = httpClientService.Members.Count(),
                    Status = JobState.Started
                };

                job.Nodes = new List<ulong>();
                job.Nodes.AddRange(n.Deps?.Select(n => n.Block.Node));

                job.WaitingOn = new List<ulong>();
                job.WaitingOn.AddRange(httpClientService.Members.Select(k => k.Key).ToArray());

                job.Status = Incoming(job, next);

                ClearWaitingOn(job);

                await jobRepository.StoreOrUpdate(job);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< JobActor.AddJob >>>: {ex.ToString()}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        private static void ClearWaitingOn(JobProto<TAttach> job)
        {
            if (job.Status.Equals(JobState.Blockmainia) || job.Status.Equals(JobState.Running))
            {
                job.WaitingOn.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <returns></returns>
        private async Task<JobProto<TAttach>> ExistingJob(TAttach next)
        {
            if (next.IsDefault())
                throw new ArgumentNullException(nameof(next));

            JobProto<TAttach> job = null;

            try
            {
                var n = next as BaseGraphProto<TAttach>;
                var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(n.Block.Hash));

                if (jobProto != null)
                {
                    jobProto.Nodes = new List<ulong>();
                    jobProto.Nodes.AddRange(n.Deps?.Select(n => n.Block.Node));

                    jobProto.WaitingOn = new List<ulong>();
                    jobProto.WaitingOn.AddRange(httpClientService.Members.Select(k => k.Key).ToArray());

                    if (!jobProto.Model.Equals(next))
                    {
                        jobProto.Status = Incoming(jobProto, next);

                        ClearWaitingOn(jobProto);
                    }

                    jobProto.Model = n;

                    await jobRepository.Include(jobProto);

                    job = await jobRepository.StoreOrUpdate(jobProto, jobProto.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< JobActor.ExistingJob >>>: {ex.ToString()}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="job"></param>
        public static JobState Incoming(JobProto<TAttach> job, TAttach next)
        {
            var n = next as BaseGraphProto<TAttach>;

            if (job.Nodes.Any())
            {

                var nodes = job.Nodes?.Except(n.Deps.Select(x => x.Block.Node));
                if (nodes.Any() != true)
                {
                    return JobState.Blockmainia;
                }
            }

            if (job.WaitingOn.Any())
            {
                var waitingOn = job.WaitingOn?.Except(n.Deps.Select(x => x.Block.Node));
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

            foreach (var next in BaseGraphProto<TAttach>.NextBlockGraph(blockHashLookup, httpClientService.NodeIdentity))
            {
                var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(next.Block.Hash));
                if (jobProto != null)
                {
                    if (!jobProto.Status.Equals(JobState.Blockmainia) &&
                        !jobProto.Status.Equals(JobState.Running) &&
                        !jobProto.Status.Equals(JobState.Polished))
                    {
                        await ExistingJob(next.Cast<TAttach>());
                    }

                    continue;
                }

                await AddJob(next.Cast<TAttach>());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="httpClientService"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IHttpClientService httpClientService) =>
            Props.Create(() => new JobActor<TAttach>(unitOfWork, httpClientService));

    }
}
