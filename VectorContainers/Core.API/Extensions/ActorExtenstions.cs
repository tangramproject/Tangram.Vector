using System;
using System.Threading;
using Akka.Actor;
using Core.API.Actors.Providers;
using Core.API.Extentions;
using Core.API.Model;
using Core.API.Network;
using Core.API.Providers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.API.Extensions
{
    public static class ActorExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IServiceCollection AddActorSystem(this IServiceCollection services, string name)
        {
            var actorSystem = ActorSystem.Create(name, "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
            services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddNetworkActorProvider<TAttach>(this IServiceCollection services)
        {
            services.AddSingleton<INetworkActorProvider, NetworkActorProvider<TAttach>>();
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
        public static IServiceCollection AddInterpretActorProvider<TAttach>(this IServiceCollection services, Func<IUnitOfWork, ISigningActorProvider, Props> invoker)
        {
            services.AddSingleton<IInterpretActorProvider<TAttach>, InterpretActorProvider<TAttach>>(sp =>
            {
                var interpretActorProvider = new InterpretActorProvider<TAttach>(
                    sp.GetService<ActorSystem>(),
                    invoker,
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<ISigningActorProvider>(),
                    sp.GetService<ILogger<InterpretActorProvider<TAttach>>>());

                return interpretActorProvider;

            });

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddProcessActorProvider<TAttach>(this IServiceCollection services)
        {
            services.AddSingleton<IProcessActorProvider<TAttach>, ProcessActorProvider<TAttach>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSipActorProvider<Startup, TAttach>(this IServiceCollection services)
        {
            services.AddSingleton<ISipActorProvider, SipActorProvider<TAttach>>(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();
                var syncProvider = sp.GetService<SyncProvider<TAttach>>();

                while (syncProvider.IsRunning)
                {
                    logger.LogInformation("Syncing node... retrying in a few seconds");
                    Thread.Sleep(5000);
                }

                var sipActorProvider = new SipActorProvider<TAttach>
                (
                    sp.GetService<ActorSystem>(),
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IHttpClientService>(),
                    sp.GetService<IInterpretActorProvider<TAttach>>(),
                    sp.GetService<IProcessActorProvider<TAttach>>(),
                    sp.GetService<ISigningActorProvider>(),
                    sp.GetService<ILogger<SipActorProvider<TAttach>>>()
                );

                return sipActorProvider;

            });

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddVerifiableFunctionsActorProvider(this IServiceCollection services)
        {
            services.AddSingleton<IVerifiableFunctionsActorProvider, VerifiableFunctionsActorProvider>(sp =>
            {
                var verifiableFunctionsActorProvider = new VerifiableFunctionsActorProvider
                (
                    sp.GetService<ActorSystem>(),
                    sp.GetService<IDataProtectionProvider>()
                );

                return verifiableFunctionsActorProvider;
            });

            return services;
        }
    }
}
