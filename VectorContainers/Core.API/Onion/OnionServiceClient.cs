using System;
using System.Net.Http;
using System.Threading.Tasks;
using Core.API.Models;
using Newtonsoft.Json;

namespace Core.API.Onion
{
    public class OnionServiceClient : IOnionServiceClient
    {
        private readonly IOnionServiceClientConfiguration _configuration;
        private readonly HttpClient _client;

        public OnionServiceClient(IOnionServiceClientConfiguration configuration, HttpClient client)
        {
            _configuration = configuration;
            _client = client;
        }

        public async Task<HiddenServiceDetails> GetHiddenServiceDetailsAsync()
        {
            var uri = new Uri(new Uri(_configuration.OnionServiceAddress), _configuration.GetHiddenServiceDetailsRoute);
            var response = await _client.GetStringAsync(uri);

            return JsonConvert.DeserializeObject<HiddenServiceDetails>(response);
        }

        public async Task<SignedHashResponse> SignHashAsync(byte[] hash)
        {
            var uri = new Uri(new Uri(_configuration.OnionServiceAddress), _configuration.SignMessageRoute);
            var response = await _client.PostAsJsonAsync(uri, hash);

            var res = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<SignedHashResponse>(res);
        }
    }
}