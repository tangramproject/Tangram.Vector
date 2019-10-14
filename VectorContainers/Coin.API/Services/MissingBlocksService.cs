using System;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.Extensions.Hosting;

namespace Coin.API.Services
{
    public class MissingBlocksService : BackgroundService
    {
        private readonly MissingBlocksProvider missingBlocksProvider;

        public MissingBlocksService(MissingBlocksProvider missingBlocksProvider)
        {
            this.missingBlocksProvider = missingBlocksProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // await missingBlocksProvider.Run(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch { }
            }
        }
    }
}
