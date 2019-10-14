using System;
using System.Threading;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.Extensions.Hosting;

namespace Coin.API.Services
{
    public class HierarchicalDataService : BackgroundService
    {
        private readonly HierarchicalDataProvider hierarchicalDataProvider;

        public HierarchicalDataService(HierarchicalDataProvider hierarchicalDataProvider)
        {
            this.hierarchicalDataProvider = hierarchicalDataProvider;
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
                    await hierarchicalDataProvider.Run(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch { }
            }
        }
    }
}
