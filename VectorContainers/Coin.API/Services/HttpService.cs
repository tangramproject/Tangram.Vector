using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Helper;
using Core.API.Membership;
using Core.API.Onion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Coin.API.Services
{
    public enum DialType
    {
        Get,
        Post
    }

    public class HttpService : IHttpService
    {
        public ulong NodeIdentity { get; private set; }
        public string GatewayUrl { get; private set; }
        public ConcurrentDictionary<ulong, string> Members { get; private set; }

        private readonly IMembershipServiceClient membershipServiceClient;
        private readonly IOnionServiceClient onionServiceClient;
        private readonly ITorClient torClient;
        private readonly ILogger logger;

        public HttpService(IMembershipServiceClient membershipServiceClient, IOnionServiceClient onionServiceClient, ITorClient torClient, IConfiguration configuration, ILogger<HttpService> logger)
        {
            this.membershipServiceClient = membershipServiceClient;
            this.onionServiceClient = onionServiceClient;
            this.torClient = torClient;
            this.logger = logger;

            var gatewaySection = configuration.GetSection("Gateway");
            GatewayUrl = gatewaySection.GetValue<string>("Url");

            Members = new ConcurrentDictionary<ulong, string>();
            NodeIdentity = Util.HostNameToHex(GetPublicKey().GetAwaiter().GetResult().ToHex());

            MaintainMembers(new CancellationToken());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetMembers()
        {
            var members = Enumerable.Empty<string>();

            try
            {
                members = (await membershipServiceClient.GetMembersAsync()).Select(x => x.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.GetMembers >>>: {ex.ToString()}");
            }

            return members;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetHostName()
        {
            string hostName = null;
            try
            {
                hostName = onionServiceClient.GetHiddenServiceDetailsAsync().GetAwaiter().GetResult().Hostname;
                hostName = hostName.Substring(0, hostName.Length - 6);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.GetHostName >>>: {ex.ToString()}");
            }

            return hostName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> GetPublicKey()
        {
            byte[] publicKey = null;

            try
            {
                var hiddenServiceDetails = await onionServiceClient.GetHiddenServiceDetailsAsync();
                publicKey = hiddenServiceDetails.PublicKey;

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.GetPublicKey >>>: {ex.ToString()}");
            }

            return publicKey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory)
        {
            return await Dial(dialType, directory, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="directory"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, object payload)
        {
            return await Dial(dialType, directory, payload, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="directory"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, string[] args)
        {
            return await Dial(dialType, directory, null, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<KeyValuePair<ulong, string>>> GetMemberIdentities()
        {
            var memberList = new List<KeyValuePair<ulong, string>>();

            try
            {
                var responses = await Dial(DialType.Get, "identity");
                foreach (var response in responses)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        continue;
                    }

                    var host = $"{response.RequestMessage.RequestUri.Scheme}://{response.RequestMessage.RequestUri.Host}:{response.RequestMessage.RequestUri.Port}";
                    var jToken = Util.ReadJToken(response, "identity");
                    var byteArray = Convert.FromBase64String(jToken.Value<string>());

                    if (byteArray.Length <= 32)
                    {
                        memberList.Add(KeyValuePair.Create(Util.HostNameToHex(byteArray.ToHex()), host));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.GetMemberIdentities >>>: {ex.ToString()}");
            }

            return memberList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<ulong, string> RandomizedIP()
        {
            var random = new Random();
            var pos = random.Next(Members.Count);

            return Members.ElementAt(pos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        private void MaintainMembers(CancellationToken stoppingToken)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var members = await GetMemberIdentities();
                        members.ForEach(member => { Members.TryAdd(member.Key, member.Value); });

                        var exptected = Members.Except(members);
                        if (exptected.Any())
                        {
                            exptected.ForEach(member => { Members.TryRemove(member.Key, out string value); });
                        }

                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }
                    catch (Exception ex)
                    {

                        logger.LogError($"<<< HttpService.MaintainMembers >>>: {ex.ToString()}");
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, object payload, string[] args)
        {
            var dialTasks = new List<Task<HttpResponseMessage>>();
            var responseTasks = new List<Task<HttpResponseMessage>>();

            try
            {
                var members = await GetMembers();
                foreach (var member in members)
                {
                    dialTasks.Add(Task.Run(() =>
                    {
                        var path = directory;

                        if (args != null)
                        {
                            path = string.Format("{0}{1}", directory, string.Join(string.Empty, args));
                        }

                        Task<HttpResponseMessage> response = null;

                        var uri = new Uri(new Uri(member), path);

                        response = dialType switch
                        {
                            DialType.Get => torClient.GetAsync(uri, new CancellationToken()),
                            _ => torClient.PostAsJsonAsync(uri, payload),
                        };

                        responseTasks.Add(response);

                        return response;
                    }));
                }

                try
                {
                    await Task.WhenAll(dialTasks.ToArray());
                }
                catch { }

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.Dial >>>: {ex.ToString()}");
            }

            return await Task.WhenAll(responseTasks);
        }
    }
}
