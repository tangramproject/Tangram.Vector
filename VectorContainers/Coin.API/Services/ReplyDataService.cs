using System;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class ReplyDataService : BackgroundService
    {
        private readonly ReplyProvider replyProvider;
        private readonly ILogger logger;

        public ReplyDataService(ReplyProvider replyProvider, ILogger<ReplyProvider> logger)
        {
            this.replyProvider = replyProvider;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await replyProvider.Run(stoppingToken);
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< ReplyDataService >>>: {ex.ToString()}");
            }

        }
    }
}
