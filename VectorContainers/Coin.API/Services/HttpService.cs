using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.ActorProviders;
using Core.API.Consensus;
using Core.API.Helper;
using Core.API.LibSodium;
using Core.API.Membership;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Onion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ISigningActorProvider signingActorProvider;
        private readonly BlockmainiaOptions blockmainiaOptions;
        private readonly ILogger logger;

        private CancellationTokenSource cancellationTokenSource;

        public HttpService(IMembershipServiceClient membershipServiceClient, IOnionServiceClient onionServiceClient,
            ITorClient torClient, ISigningActorProvider signingActorProvider, IConfiguration configuration,
            IOptions<BlockmainiaOptions> blockmainiaOptions, ILogger<HttpService> logger)
        {
            this.membershipServiceClient = membershipServiceClient;
            this.onionServiceClient = onionServiceClient;
            this.torClient = torClient;
            this.signingActorProvider = signingActorProvider;
            this.blockmainiaOptions = blockmainiaOptions.Value;
            this.logger = logger;

            var gatewaySection = configuration.GetSection("Gateway");
            GatewayUrl = gatewaySection.GetValue<string>("Url");

            Members = new ConcurrentDictionary<ulong, string>();

            SetNodeIdentity();

            cancellationTokenSource = new CancellationTokenSource();

            MaintainMembers(cancellationTokenSource.Token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ITorClient GetTorClient() => torClient;

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
                hostName = hostName[0..^6];
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
        /// Post
        /// </summary>
        /// <param name="address"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Dial(string address, object payload)
        {
            return (await Dial(DialType.Post, new string[] { address }, null, payload, null)).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="address"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Dial(DialType dialType, string address, string directory)
        {
            return (await Dial(dialType, new string[] { address }, directory, null, null)).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="addresses"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, IEnumerable<string> addresses, string directory)
        {
            return await Dial(dialType, addresses, directory, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialType"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory)
        {
            return await Dial(dialType, await GetMembers(), directory, null, null);
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
            return await Dial(dialType, await GetMembers(), directory, payload, null);
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
            return await Dial(dialType, await GetMembers(), directory, null, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public IdentityProto GetIdentity(ulong peer)
        {
            return new IdentityProto
            {
                Client = peer,
                Nonce = Cryptography.RandomBytes(36),
                Server = NodeIdentity,
                Timestamp = DateTimeOffset.UtcNow.Ticks
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<PayloadProto> SignPayload(object value)
        {
            Core.API.Models.SignedHashResponse signedHashResponse;
            string agent = "tgm/0.0.1";
            byte[] payload;

            if (value is IdentityProto)
            {
                payload = Util.SerializeProto((IdentityProto)value);
                signedHashResponse = await signingActorProvider.Sign(new SignedHashMessage(payload));

                return new PayloadProto
                {
                    Agent = agent,
                    Payload = payload,
                    PublicKey = signedHashResponse.PublicKey,
                    Signature = signedHashResponse.Signature
                };
            }

            return default;
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

                    var peer = await VerifyPeer(response);
                    if (string.IsNullOrEmpty(peer.Value))
                    {
                        continue;
                    }

                    memberList.Add(peer);
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
        /// <param name="node"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<ulong, string>> GetMemberIdentity(ulong node)
        {
            var peer = new KeyValuePair<ulong, string>();

            try
            {
                var member = Members.FirstOrDefault(m => m.Key.Equals(node));
                if (string.IsNullOrEmpty(member.Value))
                {
                    return default;
                }

                var response = await Dial(DialType.Get, member.Value, "identity");
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                peer = await VerifyPeer(response);
                if (string.IsNullOrEmpty(peer.Value))
                {
                    return default;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.GetMemberIdentity >>>: {ex.ToString()}");
            }

            return peer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public KeyValuePair<ulong, string> GetFullNodeIdentity(HttpResponseMessage response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            var scheme = response.RequestMessage.RequestUri.Scheme;
            var authority = response.RequestMessage.RequestUri.Authority;
            var identity = Members.FirstOrDefault(k => k.Value.Equals($"{scheme}://{authority}"));

            return identity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<ulong, string>> VerifyPeer(HttpResponseMessage response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            var peer = new KeyValuePair<ulong, string>();
            var host = $"{response.RequestMessage.RequestUri.Scheme}://{response.RequestMessage.RequestUri.Host}:{response.RequestMessage.RequestUri.Port}";
            var jToken = Util.ReadJToken(response, "identity");
            var byteArray = Convert.FromBase64String(jToken.Value<string>());

            if (byteArray.Length > 0)
            {
                var payload = Util.DeserializeProto<PayloadProto>(byteArray);

                var success = await signingActorProvider.VerifiySignature(new VerifiySignatureMessage(payload.Signature, payload.Payload, payload.PublicKey));
                if (success)
                {
                    var identity = Util.DeserializeProto<IdentityProto>(payload.Payload);
                    if (!identity.Server.Equals(NodeIdentity))
                    {
                        logger.LogError($"Node: Mismatched server field in identity: expected {NodeIdentity}, got {identity.Server}");
                        return default;
                    }

                    var diff = DateTime.UtcNow.Subtract(new TimeSpan(identity.Timestamp)).Ticks;
                    if (diff < 0)
                    {
                        diff = -diff;
                    }

                    if (diff > blockmainiaOptions.NonceExpiration)
                    {
                        logger.LogError($"Node: Timestamp in client identity is outside of the max clock skew range {diff}");
                        return default;
                    }

                    return KeyValuePair.Create(Util.HashToId(payload.PublicKey.ToHex()), host);
                }
            }

            return peer;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetNodeIdentity()
        {
            try
            {
                NodeIdentity = Util.HashToId(GetPublicKey().GetAwaiter().GetResult().ToHex());
            }
            catch (Exception ex)
            {
                logger.LogCritical($"<<< HttpService.SetNodeIdentity >>>: {ex.ToString()}");
                Environment.Exit(1);
            }
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
        private async Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, IEnumerable<string> addresses, string directory, object payload, string[] args)
        {
            var dialTasks = new List<Task<HttpResponseMessage>>();
            var responseTasks = new List<Task<HttpResponseMessage>>();

            try
            {
                foreach (var address in addresses)
                {
                    dialTasks.Add(Task.Run(() =>
                    {
                        var path = directory;

                        if (args != null)
                        {
                            path = string.Format("{0}{1}", directory, string.Join(string.Empty, args));
                        }

                        Task<HttpResponseMessage> response = null;

                        var uri = new Uri(new Uri(address), path);

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
                catch (Exception ex)
                {
                    logger.LogError($"<<< HttpService.Dial >>>: {ex.ToString()}");
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< HttpService.Dial >>>: {ex.ToString()}");
            }

            return await Task.WhenAll(responseTasks);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (cancellationTokenSource != null)
                    {
                        cancellationTokenSource.Cancel();
                        cancellationTokenSource.Dispose();
                        cancellationTokenSource = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
