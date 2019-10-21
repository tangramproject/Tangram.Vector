using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IBlockIDRepository : IRepository<BlockIDProto>
    {
        Task<bool> HasCoin(string hash);
        Task<IEnumerable<BlockIDProto>> GetRange(int skip, int take);
        Task<int> Count(ulong node);
    }
}
