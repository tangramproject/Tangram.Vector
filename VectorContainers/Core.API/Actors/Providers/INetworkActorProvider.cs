using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Model;

namespace Core.API.Actors.Providers
{
    public interface INetworkActorProvider
    {
        Task<int> BlockHeight();
        Task<IEnumerable<NodeBlockCountProto>> FullNetworkBlockHeight();
        Task<ulong> NetworkBlockHeight();
    }
}
