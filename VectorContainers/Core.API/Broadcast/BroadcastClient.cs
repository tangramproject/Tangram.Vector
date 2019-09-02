using Core.API.Membership;
using Core.API.Onion;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Core.API.Broadcast
{
    public class BroadcastClient : IBroadcastClient
    {
        private readonly ITorClient _torClient;
        private readonly IMembershipServiceClient _membershipServiceClient;

        public BroadcastClient(ITorClient torClient, IMembershipServiceClient membershipServiceClient)
        {
            _torClient = torClient;
            _membershipServiceClient = membershipServiceClient;
        }

        public async Task BroadcastMessageAsync(object message, Uri route)
        {
            var members = await _membershipServiceClient.GetMembersAsync().ConfigureAwait(false);

            foreach (var member in members)
            {
                _ = Task.Factory.StartNew(async () =>
                  {
                      var uri = new Uri(new Uri(member.Endpoint), route);

                      await _torClient.PostAsync(uri,
                          new StringContent(JsonConvert.SerializeObject(message),
                              Encoding.UTF8, "application/json"),
                          new System.Threading.CancellationToken());
                  });
            }
        }
    }
}
