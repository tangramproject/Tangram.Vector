// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Threading.Tasks;

namespace TGMCore.Model
{
    public interface IBaseBlockIDRepository<TAttach> : IRepository<BaseBlockIDProto<TAttach>>
    {
        Task<IEnumerable<BaseBlockIDProto<TAttach>>> GetRange(int skip, int take);
        Task<int> Count(ulong node);
    }
}
