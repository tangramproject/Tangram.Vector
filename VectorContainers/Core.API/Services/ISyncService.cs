using System.Threading.Tasks;

namespace Core.API.Services
{
    public interface ISyncService
    {
        Task Synchronize(long numberOfBlocks);
    }
}
