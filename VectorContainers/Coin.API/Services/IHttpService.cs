using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Coin.API.Services
{
    public interface IHttpService
    {
        ulong NodeIdentity { get; }
        string GatewayUrl { get; }
        ConcurrentDictionary<ulong, string> Members { get; }
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, IEnumerable<string> addresses, string directory);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string address, string directory);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, object payload);
        Task<IEnumerable<HttpResponseMessage>> Dial(DialType dialType, string directory, string[] args);
        Task<IEnumerable<string>> GetMembers();
        string GetHostName();
        Task<byte[]> GetPublicKey();
        Task<List<KeyValuePair<ulong, string>>> GetMemberIdentities();
        KeyValuePair<ulong, string> GetFullNodeIdentity(HttpResponseMessage response);
   }
}