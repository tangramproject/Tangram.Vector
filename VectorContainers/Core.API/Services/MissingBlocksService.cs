using System;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Providers;
using Microsoft.Extensions.Hosting;

namespace Core.API.Services
{
    public class MissingBlocksService<TAttach> : BackgroundService
    {
        private readonly MissingBlocksProvider<TAttach> missingBlocksProvider;

        public MissingBlocksService(MissingBlocksProvider<TAttach> missingBlocksProvider)
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
