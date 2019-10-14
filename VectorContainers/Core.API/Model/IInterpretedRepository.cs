using System;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IInterpretedRepository : IRepository<InterpretedProto>
    {
        Task<InterpretedProto> Get();
        Task<ulong> GetRound();
        void Store(ulong consumed, ulong round);
    }
}
