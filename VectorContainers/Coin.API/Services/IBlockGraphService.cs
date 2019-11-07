using System.Threading.Tasks;
using Core.API.Model;

namespace Coin.API.Services
{
    public interface IBlockGraphService
    {
        Task<BlockGraphProto> SetBlockGraph(BlockGraphProto blockGraph);
    }
}
