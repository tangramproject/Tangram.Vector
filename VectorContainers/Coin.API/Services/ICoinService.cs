using System.Threading.Tasks;
using Coin.API.Model;

namespace Coin.API.Services
{
    public interface ICoinService
    {
        Task<byte[]> AddCoin(CoinProto coin);
        Task<byte[]> GetCoin(string key);
        Task<byte[]> GetCoins(string key, int skip, int take);
        Task<byte[]> GetCoins(string key);
        Task<byte[]> GetCoins(int skip, int take);
    }
}
