using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core.API.Membership
{
    public class MembershipServiceClient : IMembershipServiceClient
    {
        private readonly IConfiguration _configuration;

        private readonly string _membershipServiceAddress;
        private readonly string _membersRoute;

        public MembershipServiceClient(IConfiguration configuration)
        {
            _configuration = configuration;

            var membershipSection = configuration.GetSection(MembershipConstants.ConfigSection);

            _membershipServiceAddress =
                membershipSection.GetValue<string>(MembershipConstants.MembershipServiceAddress);

            _membersRoute = membershipSection.GetValue<string>(MembershipConstants.MembersRoute);
        }

        public async Task<IEnumerable<INode>> GetMembersAsync()
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(_membershipServiceAddress), _membersRoute);
                var response = await client.GetStringAsync(uri).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<List<Node>>(response);
            }
        }

        public IEnumerable<INode> GetMembers()
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(_membershipServiceAddress), _membersRoute);
                var response = client.GetStringAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();

                return JsonConvert.DeserializeObject<List<Node>>(response);
            }
        }
    }
}
