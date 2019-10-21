using System;
using System.Collections.Concurrent;
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
    public class MissingBlocksProvider
    {
        public bool IsBusy { get; private set; }

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ILogger logger;

        public MissingBlocksProvider(IUnitOfWork unitOfWork, IHttpService httpService, ILogger<MissingBlocksProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.logger = logger;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                var jobs = await unitOfWork.Job.GetWhere(x => x.Status == JobState.Queued);

                foreach (var next in jobs)
                {
                    var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(next.Hash));
                    if (jobProto != null)
                    {
                        // Future switch on job states
                        if (jobProto.Status.Equals(JobState.Queued))
                        {
                            var ts = new TimeSpan(DateTime.UtcNow.Ticks - DateTimeOffset.FromUnixTimeSeconds(jobProto.Epoch).Ticks);
                            double delta = Math.Abs(ts.TotalSeconds);
                            if (delta > 30)
                            {
                                await PullRemote().ContinueWith(async t => await PushLocal(t.Result));
                            }
                        }

                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MissingBlocksProvider.Run >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<ConcurrentDictionary<ulong, BlockGraphProto>> PullRemote()
        {
            var allTasks = new List<Task<HttpResponseMessage>>();
            var blocks = new ConcurrentDictionary<ulong, BlockGraphProto>();

            var jobs = await unitOfWork.Job.GetWhere(x => x.Status == JobState.Queued);
            foreach (var job in jobs)
            {
                await MarkAs(job, JobState.Dialling);

                foreach (var member in httpService.Members)
                {
                    allTasks.Add(Task.Run(async () =>
                    {
                        HttpResponseMessage response = null;

                        response = await httpService.Dial(DialType.Get, member.Value, $"mempool/{job.BlockGraph.Block.Hash}/{job.BlockGraph.Block.Round}");
                        response.EnsureSuccessStatusCode();

                        var jToken = Util.ReadJToken(response, "protobuf");
                        var byteArray = Convert.FromBase64String(jToken.Value<string>());

                        if (byteArray.Length > 0)
                        {
                            var scheme = response.RequestMessage.RequestUri.Scheme;
                            var authority = response.RequestMessage.RequestUri.Authority;
                            var identity = httpService.Members.FirstOrDefault(k => k.Value.Equals($"{scheme}://{authority}"));

                            blocks.TryAdd(identity.Key, Util.DeserializeProto<BlockGraphProto>(byteArray));
                        }

                        return response;
                    }));
                }
            }

            try
            {
                await Task.WhenAll(allTasks.ToArray());
            }
            catch { }


            return blocks;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private async Task PushLocal(ConcurrentDictionary<ulong, BlockGraphProto> blocks)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            if (blocks.Any() != true)
            {
                return;
            }

            try
            {
                var payload = blocks.Select(k => k.Value);

                var response = await httpService.Dial($"{httpService.GatewayUrl}/blockgraphs", Util.SerializeProto(payload));
                if (response.IsSuccessStatusCode)
                {
                    var jToken = Util.ReadJToken(response, "protobufs");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());

                    if (byteArray.Length > 0)
                    {
                        var blockHashes = Util.DeserializeListProto<BlockInfoProto>(byteArray);
                        if (blockHashes.Any())
                        {
                            if (payload.Count() == blockHashes.Count())
                            {
                                var group = blocks.GroupBy(h => h.Value.Block.Hash);
                                foreach (var next in group)
                                {
                                    var hash = next.FirstOrDefault().Value.Block.Hash;
                                    var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(hash));
                                    if (jobProto != null)
                                    {
                                        jobProto.Status = JobState.Answered;

                                        next.ForEach((k) =>
                                        {
                                            HierarchicalDataProvider.AddDependency(jobProto.BlockGraph, k.Value);
                                        });

                                        jobProto.TotalNodes = next.Count();

                                        jobProto.Nodes.AddRange(next.Select(n => n.Key));
                                        jobProto.WaitingOn.Clear();

                                        await unitOfWork.Job.StoreOrUpdate(jobProto, jobProto.Id);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        await Fallback(blocks);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MissingBlocksProvider.PushLocal >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private async Task Fallback(ConcurrentDictionary<ulong, BlockGraphProto> blocks)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            if (blocks.Any() != true)
            {
                return;
            }

            try
            {
                foreach (var next in blocks)
                {
                    var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(next.Value.Block.Hash));
                    if (jobProto != null)
                    {
                        await MarkAs(jobProto, JobState.Queued);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MissingBlocksProvider.Fallback >>>: {ex.ToString()}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private async Task MarkAs(JobProto job, JobState state)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                if (job == null)
                {
                    return;
                }

                var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(job.Hash));
                if (jobProto != null)
                {
                    jobProto.Status = state;

                    await unitOfWork.Job.StoreOrUpdate(jobProto, jobProto.Id);
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MissingBlocksProvider.MarkAs >>>: {ex.ToString()}");
            }
        }
    }
}
