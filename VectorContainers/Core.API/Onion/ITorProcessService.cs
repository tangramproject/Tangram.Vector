using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Models;
using Newtonsoft.Json.Linq;

namespace Core.API.Onion
{
    public interface ITorProcessService
    {
        void ChangeCircuit(SecureString password);
        void GenerateHashPassword(SecureString password);
        void StartOnion();
        Task<HiddenServiceDetails> GetHiddenServiceDetailsAsync();
        Task<SignedHashResponse> SignedHashAsync(byte[] hash);
    }
}
