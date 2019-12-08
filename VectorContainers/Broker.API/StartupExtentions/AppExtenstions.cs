using System;
using Broker.API.Providers;
using Broker.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Broker.API.StartupExtentions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMqttService(this IServiceCollection services)
        {
            services.AddSingleton<MQTTServerProvider>();
            services.AddHostedService<MqttService>();
            return services;
        }
    }
}
