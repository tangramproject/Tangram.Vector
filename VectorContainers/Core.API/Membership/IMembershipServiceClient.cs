using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Membership
{
    public interface IMembershipServiceClient
    {
        IEnumerable<INode> GetMembers();
        Task<IEnumerable<INode>> GetMembersAsync();
    }
}