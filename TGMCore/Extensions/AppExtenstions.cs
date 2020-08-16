// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using TGMCore.Model;
using TGMCore.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace TGMCore.Extensions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddBlockGraphService<TAttach>(this ContainerBuilder builder)
        {
            builder.RegisterType<BlockGraphService<TAttach>>().As<IBlockGraphService<TAttach>>();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddBActorService<TAttach>(this ContainerBuilder builder)
        {
            builder.RegisterType<ActorService>().As<IActorService>().SingleInstance();
            return builder;
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
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddDbContext(this ContainerBuilder builder)
        {
            builder.RegisterType<DbContext>().As<IDbContext>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddUnitOfWork(this ContainerBuilder builder)
        {
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDataKeysProtection(this IServiceCollection services, string path)
        {
            services.AddSingleton<IDataProtectionKeyRepository, DataProtectionKeyRepository>(sp =>
            {
                var dataProtectionKeyRepository = new DataProtectionKeyRepository(sp.GetService<IDbContext>());
                return dataProtectionKeyRepository;
            });

            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(path))
                .SetApplicationName("tangram")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7));

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddDataKeysProtection(this ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var dataProtectionKeyRepository = new DataProtectionKeyRepository(c.Resolve<IDbContext>());
                return dataProtectionKeyRepository;
            })
            .As<IDataProtectionKeyRepository>();

            return builder;
        }
    }
}
