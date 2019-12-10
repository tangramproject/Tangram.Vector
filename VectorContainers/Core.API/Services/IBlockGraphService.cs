using System.Threading.Tasks;
using Core.API.Model;

namespace Core.API.Services
{
    public interface IBlockGraphService<TAttach>
    {
        Task<BaseGraphProto<TAttach>> SetBlockGraph(BaseGraphProto<TAttach> blockGraph);
    }
}
