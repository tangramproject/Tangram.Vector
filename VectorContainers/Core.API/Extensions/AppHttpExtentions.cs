using System;
using System.Net;
using System.Net.Http;
using Core.API.Actors.Providers;
using Core.API.Broadcast;
using Core.API.Consensus;
using Core.API.Extentions;
using Core.API.Helper;
using Core.API.Membership;
using Core.API.Network;
using Core.API.Onion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MihaZupan;

namespace Core.API.Extensions
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
        public static IServiceCollection AddHttpClientService(this IServiceCollection services, string gateway)
        {
            services.AddSingleton<IHttpClientService, HttpClientService>(sp =>
            {
                var httpClientService = new HttpClientService(
                    sp.GetService<IMembershipServiceClient>(),
                    sp.GetService<IOnionServiceClient>(),
                    sp.GetService<ITorClient>(),
                    sp.GetService<ISigningActorProvider>(),
                    gateway,
                    sp.GetService<IOptions<BlockmainiaOptions>>(),
                    sp.GetService<ILogger<HttpClientService>>());

                return httpClientService;
            });

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpClientHandler<Startup>(this IServiceCollection services)
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
        public static IServiceCollection AddTorClient<Startup>(this IServiceCollection services)
        {
            services.AddHttpClient<ITorClient, TorClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler((services, request) =>
                {
                    var logger = services.GetService<ILogger<Startup>>();
                    var httpService = services.GetService<IHttpClientService>();

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
