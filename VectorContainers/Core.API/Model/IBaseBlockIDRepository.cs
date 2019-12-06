using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IBaseBlockIDRepository<TAttach> : IRepository<BaseBlockIDProto<TAttach>>
    {
        Task<IEnumerable<BaseBlockIDProto<TAttach>>> GetRange(int skip, int take);
        Task<int> Count(ulong node);
    }
}
