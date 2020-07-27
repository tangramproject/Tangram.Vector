// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using TGMCore.Model;
using TGMCore.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace TGMCore.Extensions
{
    public static class AppExtenstions
    {
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
        //public static IServiceCollection AddSyncProvider<TAttach>(this IServiceCollection services, string route)
        //{
        //    services.AddSingleton(sp =>
        //    {
        //        var syncProvider = new SyncProvider<TAttach>(
        //            sp.GetService<IUnitOfWork>(),
        //            sp.GetService<IHttpClientService>(),
        //            sp.GetService<INetworkActorProvider>(),
        //            sp.GetService<IInterpretActorProvider<TAttach>>(),
        //            route,
        //            sp.GetService<ILogger<SyncProvider<TAttach>>>()
        //        );

        //        return syncProvider;
        //    });

        //    services.AddHostedService<SyncService<TAttach>>();
        //    return services;
        //}

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
