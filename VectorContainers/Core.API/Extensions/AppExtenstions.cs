using System;
using Core.API.Actors.Providers;
using Core.API.Model;
using Core.API.MQTT;
using Core.API.Network;
using Core.API.Providers;
using Core.API.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.API.Extensions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPubSubBlockGraphProvider<TAttach>(this IServiceCollection services, NodeEndPoint nodeEndPoint)
        {
            services.AddSingleton(sp =>
            {
                var pubSubProvider = new PubSubBlockGraphProvider<TAttach>(
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IHttpClientService>(),
                    sp.GetService<IBlockGraphService<TAttach>>(),
                    sp.GetService<ILogger<PubSubBlockGraphProvider<TAttach>>>(),
                    nodeEndPoint
                );

                return pubSubProvider;
            });
            services.AddHostedService<PubSubBlockGraphService<TAttach>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBlockGraphService<TAttach>(this IServiceCollection services)
        {
            services.AddTransient<IBlockGraphService<TAttach>, BlockGraphService<TAttach>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyncProvider<TAttach>(this IServiceCollection services, string route)
        {
            services.AddSingleton(sp =>
            {
                var syncProvider = new SyncProvider<TAttach>(
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IHttpClientService>(),
                    sp.GetService<INetworkActorProvider>(),
                    sp.GetService<IInterpretActorProvider<TAttach>>(),
                    route,
                    sp.GetService<ILogger<SyncProvider<TAttach>>>()
                );

                return syncProvider;
            });

            services.AddHostedService<SyncService<TAttach>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBroadcastProvider<TAttach>(this IServiceCollection services)
        {
            services.AddTransient<BroadcastProvider<TAttach>>();
            services.AddHostedService<BroadcastService<TAttach>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDbContext(this IServiceCollection services)
        {
            services.AddSingleton<IDbContext, DbContext>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.AddSingleton<IUnitOfWork, UnitOfWork>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDataKeysProtection(this IServiceCollection services)
        {
            services.AddSingleton<IDataProtectionKeyRepository, DataProtectionKeyRepository>(sp =>
            {
                var dataProtectionKeyRepository = new DataProtectionKeyRepository(sp.GetService<IDbContext>());
                return dataProtectionKeyRepository;
            });

            services
                .AddDataProtection()
                .AddKeyManagementOptions(options => options.XmlRepository = services.BuildServiceProvider().GetService<IDataProtectionKeyRepository>())
                .SetApplicationName("tangram")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7));

            return services;
        }
    }
}
