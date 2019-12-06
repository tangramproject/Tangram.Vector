using System.Threading.Tasks;
using Core.API.Messages;

namespace Core.API.Actors.Providers
{
    public interface IProcessActorProvider<TAttach>
    {
        Task<bool> Process(BlockGraphMessage<TAttach> message);
    }
}
