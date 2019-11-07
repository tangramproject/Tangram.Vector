using System.Threading.Tasks;
using Core.API.Messages;

namespace Coin.API.ActorProviders
{
    public interface ISipActorProvider
    {
        void Register(HashedMessage message);
        Task<bool> GracefulStop(GracefulStopMessge messge);
    }
}
