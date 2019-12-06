using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Extentions;
using Core.API.Model;
using Core.API.Network;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Core.API.Providers
{
    public class MissingBlocksProvider<TAttach>
    {
        public bool IsBusy { get; private set; }

        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly ILogger logger;
        private readonly IJobRepository<TAttach> jobRepository;

        public MissingBlocksProvider(IUnitOfWork unitOfWork, IHttpClientService httpClientService, ILogger<MissingBlocksProvider<TAttach>> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;
            this.logger = logger;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                var jobs = await jobRepository.GetWhere(x => x.Status == JobState.Queued);

                foreach (var next in jobs)
                {
                    var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(next.Hash));
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
        private async Task<ConcurrentDictionary<ulong, BaseGraphProto<TAttach>>> PullRemote()
        {
            var allTasks = new List<Task<HttpResponseMessage>>();
            var blocks = new ConcurrentDictionary<ulong, BaseGraphProto<TAttach>>();

            var jobs = await jobRepository.GetWhere(x => x.Status == JobState.Queued);
            foreach (var job in jobs)
            {
                await MarkAs(job, JobState.Dialling);

                foreach (var member in httpClientService.Members)
                {
                    allTasks.Add(Task.Run(async () =>
                    {
                        HttpResponseMessage response = null;

                        response = await httpClientService.Dial(DialType.Get, member.Value, $"mempool/{job.Model.Block.Hash}/{job.Model.Block.Round}");
                        response.EnsureSuccessStatusCode();

                        var jToken = Helper.Util.ReadJToken(response, "protobuf");
                        var byteArray = Convert.FromBase64String(jToken.Value<string>());

                        if (byteArray.Length > 0)
                        {
                            var scheme = response.RequestMessage.RequestUri.Scheme;
                            var authority = response.RequestMessage.RequestUri.Authority;
                            var identity = httpClientService.Members.FirstOrDefault(k => k.Value.Equals($"{scheme}://{authority}"));

                            blocks.TryAdd(identity.Key, Helper.Util.DeserializeProto<BaseGraphProto<TAttach>>(byteArray));
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
        private async Task PushLocal(ConcurrentDictionary<ulong, BaseGraphProto<TAttach>> blocks)
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

                var response = await httpClientService.Dial($"{httpClientService.GatewayUrl}/blockgraphs", Helper.Util.SerializeProto(payload));
                if (response.IsSuccessStatusCode)
                {
                    var jToken = Helper.Util.ReadJToken(response, "protobufs");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());

                    if (byteArray.Length > 0)
                    {
                        var blockHashes = Helper.Util.DeserializeListProto<BlockInfoProto>(byteArray);
                        if (blockHashes.Any())
                        {
                            if (payload.Count() == blockHashes.Count())
                            {
                                var group = blocks.GroupBy(h => h.Value.Block.Hash);
                                foreach (var next in group)
                                {
                                    var hash = next.FirstOrDefault().Value.Block.Hash;
                                    var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(hash));
                                    if (jobProto != null)
                                    {
                                        jobProto.Status = JobState.Answered;

                                        next.ForEach((k) =>
                                        {
                                            BaseGraphProto<TAttach>.AddDependency(jobProto.Model, k.Value);
                                        });

                                        jobProto.TotalNodes = next.Count();

                                        jobProto.Nodes.AddRange(next.Select(n => n.Key));
                                        jobProto.WaitingOn.Clear();

                                        await jobRepository.StoreOrUpdate(jobProto, jobProto.Id);
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
        private async Task Fallback(ConcurrentDictionary<ulong, BaseGraphProto<TAttach>> blocks)
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
                    var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(next.Value.Block.Hash));
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
        private async Task MarkAs(JobProto<TAttach> job, JobState state)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                if (job == null)
                {
                    return;
                }

                var jobProto = await jobRepository.GetFirstOrDefault(x => x.Hash.Equals(job.Hash));
                if (jobProto != null)
                {
                    jobProto.Status = state;

                    await jobRepository.StoreOrUpdate(jobProto, jobProto.Id);
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MissingBlocksProvider.MarkAs >>>: {ex.ToString()}");
            }
        }
    }
}
