using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Model;

namespace Coin.API.ActorProviders
{
    public interface INetworkActorProvider
    {
        Task<int> BlockHeight();
        Task<IEnumerable<NodeBlockCountProto>> FullNetworkBlockHeight();
        Task<ulong> NetworkBlockHeight();
    }
}
