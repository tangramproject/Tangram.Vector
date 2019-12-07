using System;
using System.Threading;
using System.Threading.Tasks;
using Broker.API.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Broker.API.Services
{
    public class MqttService: BackgroundService
    {
        private readonly MQTTServerProvider mQTTServerProvider;
        private readonly ILogger logger;

        public MqttService(MQTTServerProvider mQTTServerProvider, ILogger<MqttService> logger)
        {
            this.mQTTServerProvider = mQTTServerProvider;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await mQTTServerProvider.Run();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MqttService.ExecuteAsync >>>: {ex.ToString()}");
            }
        }
    }
}
