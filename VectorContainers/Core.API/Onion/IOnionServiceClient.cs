using System.Threading.Tasks;
using Core.API.Models;

namespace Core.API.Onion
{
    public interface IOnionServiceClient
    {
        Task<HiddenServiceDetails> GetHiddenServiceDetailsAsync();
        Task<SignedHashResponse> SignHashAsync(byte[] hash);
    }
}