using Core.API.Models;
using DotNetTor.SocksPort;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content)
        {
            return _client.PostAsJsonAsync(requestUri, content);
        }

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(Uri requestUri, T content)
        {
            return _client.PostAsJsonAsync(requestUri, content);
        }

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content,
            CancellationToken cancellationToken)
        {
            return _client.PostAsJsonAsync(requestUri, content, cancellationToken);
        }

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(
            Uri requestUri,
            T content,
            CancellationToken cancellationToken)
        {
            return _client.PostAsJsonAsync(requestUri, content, cancellationToken);
        }
    }
}
