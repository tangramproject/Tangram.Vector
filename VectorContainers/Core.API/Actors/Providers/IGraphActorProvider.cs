using System.Threading.Tasks;
using Core.API.Messages;

namespace Core.API.Actors.Providers
{
    public interface IGraphActorProvider<TAttach>
    {
        Task Process(ProcessBlockMessage<TAttach> message);
        Task RegisterAsync(HashedMessage message);
    }
}
