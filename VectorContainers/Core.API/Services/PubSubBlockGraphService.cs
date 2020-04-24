using System;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.API.Services
{
    public class PubSubBlockGraphService<TAttach> : BackgroundService
    {
        private readonly PubSubBlockGraphProvider<TAttach>  pubSubProvider;
        private readonly ILogger logger;

        public PubSubBlockGraphService(PubSubBlockGraphProvider<TAttach> pubSubProvider, ILogger<PubSubBlockGraphProvider<TAttach>> logger)
        {
            this.pubSubProvider = pubSubProvider;
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
                await pubSubProvider.Start();

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await pubSubProvider.Publish();
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< PubSubBlockGraphService >>>: {ex}");
            }

        }
    }
}
