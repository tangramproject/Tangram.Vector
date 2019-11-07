using System;
using Coin.API.Providers;
using Coin.API.Services;
using Core.API.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Coin.API.StartupExtensions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyncProvider(this IServiceCollection services)
        {
            services.AddSingleton<SyncProvider>();
            services.AddHostedService<SyncService>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBroadcastProvider(this IServiceCollection services)
        {
            services.AddSingleton<BroadcastProvider>();
            services.AddHostedService<BroadcastService>();
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
        public static IServiceCollection AddBlockGraphService(this IServiceCollection services)
        {
            services.AddTransient<IBlockGraphService, BlockGraphService>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCoinService(this IServiceCollection services)
        {
            services.AddTransient<ICoinService, CoinService>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMissingBlocksProvider(this IServiceCollection services)
        {
            services.AddSingleton<MissingBlocksProvider>();
            services.AddHostedService<MissingBlocksService>();
            return services;
        }
    }
}
