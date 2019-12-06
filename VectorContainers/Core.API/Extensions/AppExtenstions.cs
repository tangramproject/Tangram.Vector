using Core.API.Actors.Providers;
using Core.API.Model;
using Core.API.Network;
using Core.API.Providers;
using Core.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.API.Extensions
{
    public static class AppExtenstions
    {
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
    }
}
