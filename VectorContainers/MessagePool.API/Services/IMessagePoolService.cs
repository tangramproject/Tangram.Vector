using System.Threading.Tasks;
using Core.API.Model;

namespace MessagePool.API.Services
{
    public interface IMessagePoolService
    {
        Task<byte[]> AddMessage(byte[] message);
        Task<byte[]> GetMessages(string key);
        Task<byte[]> GetMessages(string key, int skip, int take);
        Task<int> Count(string key);
    }
}
