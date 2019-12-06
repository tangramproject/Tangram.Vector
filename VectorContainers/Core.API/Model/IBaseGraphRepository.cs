using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IBaseGraphRepository<TAttach> : IRepository<BaseGraphProto<TAttach>> 
    {
        Task<List<BaseGraphProto<TAttach>>> More(IEnumerable<BaseGraphProto<TAttach>> blocks);
        Task Include(IEnumerable<BaseGraphProto<TAttach>> blockGraphs, ulong node);
        Task<int> Count(ulong node);
        Task<BaseGraphProto<TAttach>> GetMax(string hash, ulong node);
        Task<BaseGraphProto<TAttach>> GetPrevious(ulong node, ulong round);
        Task<BaseGraphProto<TAttach>> GetPrevious(string hash, ulong node, ulong round);
        Task<BaseGraphProto<TAttach>> CanAdd(BaseGraphProto<TAttach> blockGraph, ulong node);
        Task<IEnumerable<P>> Where<P>(Func<P, bool> expression);
    }
}
