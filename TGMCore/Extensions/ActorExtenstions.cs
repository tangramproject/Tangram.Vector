﻿// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using Akka.Actor;
using TGMCore.Providers;
using TGMCore.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Akka.Configuration;

namespace TGMCore.Extensions
{
    public static class ActorExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IServiceCollection AddActorSystem(this IServiceCollection services, string name, string configFile)
        {
            var config = Helper.ConfigurationLoader.Load(configFile).WithFallback(ConfigurationFactory.Default());
            var actorSystem = ActorSystem.Create(name, config);

            services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);
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
                //var syncProvider = sp.GetService<SyncProvider<TAttach>>();

                //while (syncProvider.IsRunning)
                //{
                //    logger.LogInformation("Syncing node... retrying in a few seconds");
                //    Thread.Sleep(5000);
                //}

                var sipActorProvider = new SipActorProvider<TAttach>
                (
                    sp.GetService<ActorSystem>(),
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IClusterProvider>(),
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
                    sp.GetService<ISigningActorProvider>()
                );

                return verifiableFunctionsActorProvider;
            });

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddClusterProvider(this IServiceCollection services, string configFile)
        {
            services.AddSingleton<IClusterProvider, ClusterProvider>(sp =>
            {
                var config = Helper.ConfigurationLoader.Load(configFile);
                var cluserProvider = new ClusterProvider
                (
                     sp.GetService<ActorSystem>(),
                     config
                );

                return cluserProvider;
            });

            return services;
        }
    }
}
