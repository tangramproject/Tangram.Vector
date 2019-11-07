using System;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class BroadcastService : BackgroundService
    {
        private readonly BroadcastProvider broadcastProvider;
        private readonly ILogger logger;

        public BroadcastService(BroadcastProvider broadcastProvider, ILogger<BroadcastService> logger)
        {
            this.broadcastProvider = broadcastProvider;
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
                        await broadcastProvider.Run();
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
