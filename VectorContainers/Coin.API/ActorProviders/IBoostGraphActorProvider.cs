using System.Threading.Tasks;
using Core.API.Messages;

namespace Coin.API.ActorProviders
{
    public interface IBoostGraphActorProvider
    {
        Task Process(ProcessBlockMessage message);
        Task RegisterAsync(HashedMessage message);
    }
}
