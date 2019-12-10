using System;
using System.Threading;
using System.Threading.Tasks;
using Core.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.API.Services
{
    public class PubSubService<TAttach> : BackgroundService
    {
        private readonly PubSubProvider<TAttach>  pubSubProvider;
        private readonly ILogger logger;

        public PubSubService(PubSubProvider<TAttach> pubSubProvider, ILogger<PubSubProvider<TAttach>> logger)
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
                logger.LogError($"<<< PubSubService >>>: {ex.ToString()}");
            }

        }
    }
}
