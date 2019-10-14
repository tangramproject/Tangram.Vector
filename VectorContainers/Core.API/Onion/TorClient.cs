using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core.API.Onion
{
    public class TorClient : ITorClient
    {
        private readonly IOnionServiceClientConfiguration _configuration;

        public HttpClient _client { get; }

        public TorClient(IOnionServiceClientConfiguration configuration, HttpClient httpClient)
        {
            _client = httpClient;
            _configuration = configuration;
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return await _client.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            return await _client.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return await _client.PostAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsync(
            Uri requestUri,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            return await _client.PostAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content)
        {
            return await _client.PostAsJsonAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(Uri requestUri, T content)
        {
            return await _client.PostAsJsonAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content,
            CancellationToken cancellationToken)
        {
            return await _client.PostAsJsonAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(
            Uri requestUri,
            T content,
            CancellationToken cancellationToken)
        {
            return await _client.PostAsJsonAsync(requestUri, content, cancellationToken);
        }

        public async Task<byte[]> GetByteArrayAsync(string requestUri)
        {
            return await _client.GetByteArrayAsync(requestUri);
        }

        public async Task<byte[]> GetByteArrayAsync(Uri requestUri)
        {
            return await _client.GetByteArrayAsync(requestUri);
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            return await _client.GetStringAsync(requestUri);
        }

        public async Task<string> GetStringAsync(Uri requestUri)
        {
            return await _client.GetStringAsync(requestUri);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return await _client.GetAsync(requestUri, cancellationToken);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return await _client.GetAsync(requestUri, cancellationToken);
        }
    }
}
