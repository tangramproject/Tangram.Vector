using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Coin.API.Providers
{
    public class ReplyProvider
    {
        private static readonly AsyncLock markStatesAsMutex = new AsyncLock();
        private static readonly AsyncLock markRepliesAsMutex = new AsyncLock();

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly SigningProvider signingProvider;
        private readonly ILogger logger;

        public ReplyProvider(IUnitOfWork unitOfWork, IHttpService httpService, SigningProvider signingProvider, ILogger<ReplyProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.signingProvider = signingProvider;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                var blocks = await unitOfWork.BlockGraph.GetWhere(x => !x.Included);

                var moreBlocks = await unitOfWork.BlockGraph.More(blocks);
                var blockHashLookup = moreBlocks.ToLookup(i => i.Block.Hash);

                await unitOfWork.BlockGraph.Include(blocks, httpService.NodeIdentity);
                await AddOrUpdateJob(blockHashLookup);
                await Reply();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyProvider.Run >>>: {ex.ToString()}");
            }
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
                logger.LogError($"<<< ReplyProvider.AddJob >>>: {ex.ToString()}");
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
                logger.LogError($"<<< ReplyProvider.ExistingJob >>>: {ex.ToString()}");
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

            foreach (var next in HierarchicalDataProvider.NextBlockGraph(blockHashLookup, httpService.NodeIdentity))
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
        /// <returns></returns>
        private async Task Reply()
        {
            SemaphoreSlim throttler = null;

            try
            {
                var blockGraphs = await unitOfWork.BlockGraph.GetWhere(x => x.Block.Node.Equals(httpService.NodeIdentity) && x.Included && !x.Replied);
                if (blockGraphs.Any() != true)
                {
                    return;
                }

                var numberOfBlocks = 100;
                var tasks = new List<Task<HttpResponseMessage>>();
                var numberOfBatches = (int)Math.Ceiling((double)numberOfBlocks / numberOfBlocks);

                throttler = new SemaphoreSlim(int.MaxValue);

                foreach (var member in httpService.Members)
                {
                    await throttler.WaitAsync();

                    var series = new long[numberOfBatches];
                    foreach (var n in series)
                    {
                        var batch = blockGraphs.Skip((int)(n * numberOfBlocks)).Take(numberOfBlocks);
                        if (batch.Any() != true)
                        {
                            continue;
                        }

                        tasks.Add(Replies(throttler, member.Value, batch));
                    }
                }

                try
                {
                    await Task.WhenAll(tasks.ToArray());
                }
                catch { }

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyProvider.Reply >>>: {ex.ToString()}");
            }
            finally
            {
                throttler?.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="throttler"></param>
        /// <param name="member"></param>
        /// <param name="batch"></param>
        /// <returns></returns>
        private Task<HttpResponseMessage> Replies(SemaphoreSlim throttler, string member, IEnumerable<BlockGraphProto> batch)
        {
            return Task.Run(async () =>
            {
                HttpResponseMessage response = null;

                try
                {
                    var sending = batch.Select(i => i);
                    var currentBlockGraphs = Util.SerializeProto(sending);
                    var uri = new Uri(new Uri(member), "blockgraphs");

                    response = await httpService.Dial(uri.AbsoluteUri, currentBlockGraphs);
                    response.EnsureSuccessStatusCode();

                    var jToken = Util.ReadJToken(response, "protobufs");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());

                    if (byteArray.Length > 0)
                    {
                        var blockInfos = Util.DeserializeListProto<BlockInfoProto>(byteArray);
                        if (blockInfos.Any())
                        {
                            await MarkMultipleStatesAs(blockInfos, JobState.Queued);
                            await MarkMultipleRepliesAs(blockInfos, true);
                        }
                    }
                }
                catch
                {
                    var blockLookup = batch.ToLookup(i => i.Block.Hash);
                    var blockInfos = blockLookup.Select(h => new BlockInfoProto { Hash = h.Key });
                    if (blockInfos.Any())
                    {
                        await MarkMultipleStatesAs(blockInfos, JobState.Dead);
                        await MarkMultipleRepliesAs(blockInfos, false);
                    }
                }
                finally
                {
                    throttler.Release();
                }

                return response;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockInfos"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task MarkMultipleStatesAs(IEnumerable<BlockInfoProto> blockInfos, JobState state)
        {
            if (blockInfos == null)
                throw new ArgumentNullException(nameof(blockInfos));

            try
            {
                using (await markStatesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {
                        return;
                    }

                    using var session = unitOfWork.Document.OpenSession();

                    var filter = blockInfos.Select(async x => await unitOfWork.Job.GetFirstOrDefault(g => g.Hash.Equals(x.Hash)));
                    for (var i = filter.GetEnumerator(); i.MoveNext();)
                    {
                        var x = i.Current;
                        if (!x.Status.Equals(JobState.Started) &&
                            !x.Status.Equals(JobState.Queued) &&
                            !x.Status.Equals(JobState.Answered) &&
                            !x.Status.Equals(JobState.Dialling))
                        {
                            continue;
                        }

                        x.Result.Status = state;
                        session.Store(x, null, x.Result.Id);
                    }

                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyProvider.MarkMultipleStatesAs >>>: {ex.ToString()}");
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="replied"></param>
        /// <returns></returns>
        private async Task MarkMultipleRepliesAs(IEnumerable<BlockInfoProto> blockInfos, bool replied = false)
        {
            if (blockInfos == null)
                throw new ArgumentNullException(nameof(blockInfos));

            try
            {
                using (await markRepliesAsMutex.LockAsync())
                {
                    if (blockInfos.Any() != true)
                    {
                        return;
                    }

                    using var session = unitOfWork.Document.OpenSession();

                    foreach (var next in blockInfos)
                    {
                        var blockGraph = await unitOfWork.BlockGraph
                            .GetFirstOrDefault(x => x.Block.Hash.Equals(next.Hash) && x.Block.Node.Equals(httpService.NodeIdentity) && x.Block.Round.Equals(next.Round));
                        if (blockGraph != null)
                        {
                            blockGraph.Replied = replied;
                            session.Store(blockGraph, null, blockGraph.Id);
                        }
                    }

                    session.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyProvider.MarkMultipleRepliesAs >>>: {ex.ToString()}");
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        private async Task<BlockGraphProto> CreateReply(BlockGraphProto blockGraph)
        {
            if (blockGraph == null)
                throw new ArgumentNullException(nameof(blockGraph));

            try
            {
                var round = blockGraph.Block.Round;
                var signed = await signingProvider.Sign(httpService.NodeIdentity, blockGraph, round, await httpService.GetPublicKey());
                if (signed == null)
                {
                    logger.LogError($"<<< ReplyProvider.CreateReply >>>: Could not sign Reply block");
                    return null;
                }

                var stored = new BlockGraphProto
                {
                    Block = blockGraph.Block,
                    Deps = blockGraph.Deps?.Select(d => d).ToList(),
                    Prev = blockGraph.Prev ?? null,
                    Included = blockGraph.Included,
                    Replied = blockGraph.Replied
                };

                return stored;
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyProvider.CreateReply >>>: {ex.ToString()}");
            }

            return null;
        }
    }
}
