using System;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.API.Services
{
    public class SyncService<I> : BackgroundService
    {
        private readonly SyncProvider<I> syncProvider;
        private readonly ILogger logger;

        public SyncService(SyncProvider<I> syncProvider, ILogger<SyncService<I>> logger)
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
