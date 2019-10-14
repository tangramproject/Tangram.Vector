using System.Threading.Tasks;

namespace Coin.API.Services
{
    public interface ISyncService
    {
        Task Synchronize(long numberOfBlocks);
    }
}