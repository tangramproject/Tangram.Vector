using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Model;

namespace Coin.API.Services
{
    public interface ICoinService
    {
        Task<byte[]> AddCoin(byte[] coin);
        Task<byte[]> GetCoin(byte[] address);
        Task<byte[]> GetCoins(byte[] key, int skip, int take);
        Task<byte[]> GetCoins(byte[] key);
    }
}
