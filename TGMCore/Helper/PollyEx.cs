using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace TGMCore.Helper
{
    public static class PollyEx
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicyAsync(ILogger logger)
        {
            var jitterer = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.BadRequest)
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .Or<TaskCanceledException>()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(jitterer.Next(0, 100)), (result, timeSpan, retryCount, context) =>
                {
                    var message = result.Exception == null ? result.Result.StatusCode.ToString() : result.Exception.Message;
                    logger.LogWarning($"Request failed with {message}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                });
        }

        public static IAsyncPolicy<HttpResponseMessage> GetNoOpPolicyAsync()
        {
            return Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .RetryAsync(3);
        }
    }
}
