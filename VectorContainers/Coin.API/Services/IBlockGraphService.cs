using System.Threading.Tasks;
using Coin.API.Model;
using Core.API.Model;

namespace Coin.API.Services
{
    public interface IBlockGraphService
    {
        Task<BaseGraphProto<CoinProto>> SetBlockGraph(BaseGraphProto<CoinProto> blockGraph);
    }
}
