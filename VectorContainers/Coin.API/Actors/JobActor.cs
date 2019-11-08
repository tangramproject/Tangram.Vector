using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;

namespace Coin.API.Actors
{
    public class JobActor : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ILoggingAdapter logger;

        public byte[] Id { get; private set; }

        public JobActor(IUnitOfWork unitOfWork, IHttpService httpService)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;

            logger = Context.GetLogger();

            ReceiveAsync<ReliableDeliveryEnvelopeMessage<WriteMessage>>(async write =>
            {
                Sender.Tell(new ReliableDeliveryAckMessage(write.MessageId));

                await Register(new HashedMessage(write.Message.Content.FromHex()));
            });

            // ReceiveAsync<HashedMessage>(async message => await Register(message));
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

                var blocks = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Hash.Equals(message.Hash.ToHex()) && !x.Included);
                if (blocks.Any())
                {
                    var moreBlocks = await unitOfWork.BlockGraph.More(blocks);
                    var blockHashLookup = moreBlocks.ToLookup(i => i.Block.Hash);

                    await unitOfWork.BlockGraph.Include(blocks, httpService.NodeIdentity);
                    await AddOrUpdateJob(blockHashLookup);
                }

                Context.Stop(Self);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< ReplyProvider.Run >>>: {ex.ToString()}");
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
        private async Task<JobProto> AddJob(BlockGraphProto next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            JobProto job = null;

            try
            {
                job = new JobProto
                {
                    Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Hash = next.Block.Hash,
                    BlockGraph = next,
                    ExpectedTotalNodes = 4,
                    Node = httpService.NodeIdentity,
                    TotalNodes = httpService.Members.Count(),
                    Status = JobState.Started
                };

                job.Nodes = new List<ulong>();
                job.Nodes.AddRange(next.Deps?.Select(n => n.Block.Node));

                job.WaitingOn = new List<ulong>();
                job.WaitingOn.AddRange(httpService.Members.Select(k => k.Key).ToArray());

                job.Status = Incoming(job, next);

                ClearWaitingOn(job);

                await unitOfWork.Job.StoreOrUpdate(job);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< ReplyProvider.AddJob >>>: {ex.ToString()}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        private static void ClearWaitingOn(JobProto job)
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
        private async Task<JobProto> ExistingJob(BlockGraphProto next)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            JobProto job = null;

            try
            {
                var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(next.Block.Hash));
                if (jobProto != null)
                {
                    jobProto.Nodes = new List<ulong>();
                    jobProto.Nodes.AddRange(next.Deps?.Select(n => n.Block.Node));

                    jobProto.WaitingOn = new List<ulong>();
                    jobProto.WaitingOn.AddRange(httpService.Members.Select(k => k.Key).ToArray());

                    if (!jobProto.BlockGraph.Equals(next))
                    {
                        jobProto.Status = Incoming(jobProto, next);

                        ClearWaitingOn(jobProto);
                    }

                    jobProto.BlockGraph = next;

                    await unitOfWork.Job.Include(jobProto);

                    job = await unitOfWork.Job.StoreOrUpdate(jobProto, jobProto.Id);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< ReplyProvider.ExistingJob >>>: {ex.ToString()}");
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="job"></param>
        public static JobState Incoming(JobProto job, BlockGraphProto next)
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
        private async Task AddOrUpdateJob(ILookup<string, BlockGraphProto> blockHashLookup)
        {
            if (blockHashLookup.Any() != true)
            {
                return;
            }

            foreach (var next in BlockGraphProto.NextBlockGraph(blockHashLookup, httpService.NodeIdentity))
            {
                var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(next.Block.Hash));
                if (jobProto != null)
                {
                    if (!jobProto.Status.Equals(JobState.Blockmainia) &&
                        !jobProto.Status.Equals(JobState.Running) &&
                        !jobProto.Status.Equals(JobState.Polished))
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
        /// <param name="httpService"></param>
        /// <returns></returns>
        public static Props Props(IUnitOfWork unitOfWork, IHttpService httpService) =>
            Akka.Actor.Props.Create(() => new JobActor(unitOfWork, httpService));
    }
}
