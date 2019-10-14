using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IBlockGraphRepository : IRepository<BlockGraphProto>
    {
        Task<IEnumerable<BlockGraphProto>> GetNotIncluded();
        Task<List<BlockGraphProto>> More(IEnumerable<BlockGraphProto> blocks);
        Task<IEnumerable<BlockGraphProto>> GetAny(string hash);
        Task Include(IEnumerable<BlockGraphProto> blockGraphs, ulong node);
        Task<IEnumerable<BlockGraphProto>> GetNotReplied(ulong node);
        Task<IEnumerable<BlockGraphProto>> GetMany(string hash, ulong node);
        Task<IEnumerable<BlockGraphProto>> GetMany(string hash, ulong node, ulong round);
        Task<int> Count(ulong node);
        Task<BlockGraphProto> GetMax(string hash, ulong node);
        Task<BlockGraphProto> GetPrevious(ulong node, ulong round);
        Task<BlockGraphProto> Get(string hash, ulong node, ulong round);
    }
}
