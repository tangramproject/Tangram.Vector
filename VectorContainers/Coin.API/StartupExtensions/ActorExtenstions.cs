using System;
using System.Threading;
using Akka.Actor;
using Coin.API.ActorProviders;
using Coin.API.Providers;
using Coin.API.Services;
using Core.API.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Coin.API.StartupExtensions
{
    public static class ActorExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddActorSystem(this IServiceCollection services)
        {
            var actorSystem = ActorSystem.Create("coinapi", "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
            services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddNetworkActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<INetworkActorProvider, NetworkActorProvider>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSigningActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<ISigningActorProvider, SigningActorProvider>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddInterpretActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<IInterpretActorProvider, InterpretActorProvider>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddProcessBlockActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<IProcessBlockActorProvider, ProcessBlockActorProvider>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSupervisorActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<ISipActorProvider, SipActorProvider>(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();
                var syncProvider = sp.GetService<SyncProvider>();

                while (syncProvider.IsRunning)
                {
                    logger.LogInformation("Syncing node... retrying in a few seconds");
                    Thread.Sleep(5000);
                }

                var sipActorProvider = new SipActorProvider
                (
                    sp.GetService<ActorSystem>(),
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IHttpService>(),
                    sp.GetService<IInterpretActorProvider>(),
                    sp.GetService<IProcessBlockActorProvider>(),
                    sp.GetService<ISigningActorProvider>(),
                    sp.GetService<ILogger<SipActorProvider>>()
                );

                return sipActorProvider;

            });

            return services;
        }
    }
}
