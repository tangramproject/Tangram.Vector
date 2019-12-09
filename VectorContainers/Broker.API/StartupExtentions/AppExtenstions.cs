using System;
using Broker.API.Providers;
using Broker.API.Services;
using Core.API.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Broker.API.StartupExtentions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IServiceCollection AddMqttService(this IServiceCollection services, int port)
        {
            services.AddSingleton(sp =>
            {
                var mQTTServerProvider = new MQTTServerProvider(sp.GetService<IHttpClientService>(), sp.GetService<ILogger<MQTTServerProvider>>(), port);
                return mQTTServerProvider;
            });

            services.AddHostedService<MqttService>();

            return services;
        }
    }
}
