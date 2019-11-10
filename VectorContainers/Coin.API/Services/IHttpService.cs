using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Core.API.Model;
using Core.API.Onion;

namespace Coin.API.Services
{
    public interface IHttpService
    {
        ITorClient GetTorClient();
        ulong NodeIdentity { get; }
        byte[] PublicKey { get; }
        string GatewayUrl { get; }
        ConcurrentDictionary<ulong, string> Members { get; }
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, IEnumerable<string> addresses, string directory);
        Task<HttpResponseMessage> Dial(DialType dialType, string address, string directory);
        Task<HttpResponseMessage> Dial(string address, object payload);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, object payload);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, string[] args);
        Task<IEnumerable<string>> GetMembers();
        string GetHostName();
        Task<List<KeyValuePair<ulong, string>>> GetMemberIdentities();
        KeyValuePair<ulong, string> GetFullNodeIdentity(HttpResponseMessage response);
        IdentityProto GetIdentity(ulong peer);
        Task<PayloadProto> SignPayload(object value);
        Task<KeyValuePair<ulong, string>> GetMemberIdentity(ulong node);
        Task<KeyValuePair<ulong, string>> VerifyPeer(HttpResponseMessage response);
        void Dispose();
   }
}