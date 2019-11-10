using System;
using System.Net;
using System.Net.Http;
using Coin.API.Services;
using Core.API.Broadcast;
using Core.API.Membership;
using Core.API.Onion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MihaZupan;
using Core.API.Helper;

namespace Coin.API.StartupExtensions
{
    public static class AppHttpExtentions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBroadcastClient(this IServiceCollection services)
        {
            services.AddTransient<IBroadcastClient, BroadcastClient>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOnionServiceClientConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IOnionServiceClientConfiguration, OnionServiceClientConfiguration>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOnionServiceClient(this IServiceCollection services)
        {
            services.AddHttpClient<IOnionServiceClient, OnionServiceClient>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpService(this IServiceCollection services)
        {
            services.AddSingleton<IHttpService, HttpService>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpClientHandler(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();

                //var onionStarted = sp.GetService<IOnionServiceClient>()
                //                     .IsTorStartedAsync()
                //                     .GetAwaiter()
                //                     .GetResult();

                //while (!onionStarted)
                //{
                //    logger.LogWarning("Unable to verify Tor is started... retrying in a few seconds");
                //    Thread.Sleep(5000);
                //    onionStarted = sp.GetService<IOnionServiceClient>()
                //                     .IsTorStartedAsync()
                //                     .GetAwaiter()
                //                     .GetResult();
                //}

                //logger.LogInformation("Tor is started... configuring Socks Port Handler");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var onionServiceClientConfiguration = sp.GetService<IOnionServiceClientConfiguration>();

                var proxy = new HttpToSocks5Proxy(new[]
                {
                    new ProxyInfo(onionServiceClientConfiguration.SocksHost, onionServiceClientConfiguration.SocksPort)
                });

                var handler = new HttpClientHandler { Proxy = proxy };

                return handler;
            });
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTorClient(this IServiceCollection services)
        {
            services.AddHttpClient<ITorClient, TorClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler((services, request) =>
                {
                    var logger = services.GetService<ILogger<Startup>>();
                    var httpService = services.GetService<IHttpService>();

                    request.Headers.Add("x-pub", httpService.PublicKey.ToHex());

                    return request.Method == HttpMethod.Get
                        ? PollyEx.GetRetryPolicyAsync(logger)
                        : PollyEx.GetNoOpPolicyAsync();
                });

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMembershipServiceClient(this IServiceCollection services)
        {
            services.AddTransient<IMembershipServiceClient, MembershipServiceClient>();
            return services;
        }
    }
}
