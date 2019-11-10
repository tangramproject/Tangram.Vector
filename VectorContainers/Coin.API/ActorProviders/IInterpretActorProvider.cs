using System.Threading.Tasks;
using Core.API.Messages;

namespace Coin.API.ActorProviders
{
    public interface IInterpretActorProvider
    {
        Task<bool> Interpret(InterpretBlocksMessage message);
    }
}
