using System;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class SyncService : BackgroundService
    {
        private readonly SyncProvider syncProvider;
        private readonly ILogger logger;

        public SyncService(SyncProvider syncProvider, ILogger<SyncService> logger)
        {
            this.syncProvider = syncProvider;
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
                await syncProvider.SynchronizeCheck();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SyncService >>>: {ex.ToString()}");
            }
        }
    }
}
