using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IBlockGraphRepository : IRepository<BlockGraphProto>
    {
        Task<List<BlockGraphProto>> More(IEnumerable<BlockGraphProto> blocks);
        Task Include(IEnumerable<BlockGraphProto> blockGraphs, ulong node);
        Task<int> Count(ulong node);
        Task<BlockGraphProto> GetMax(string hash, ulong node);
        Task<BlockGraphProto> GetPrevious(ulong node, ulong round);
        Task<BlockGraphProto> GetPrevious(string hash, ulong node, ulong round);
        Task<BlockGraphProto> CanAdd(BlockGraphProto blockGraph, ulong node);
    }
}
