// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using Akka.Actor;
using TGMCore.Providers;
using TGMCore.Model;
using Microsoft.Extensions.Logging;
using Autofac;
using TGMCore.Services;

namespace TGMCore.Extensions
{
    public static class ActorExtenstions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="name"></param>
        /// <param name="configFile"></param>
        /// <returns></returns>
        public static ContainerBuilder AddActorSystem(this ContainerBuilder builder, string name, string configFile)
        {
            builder.Register(c =>
            {
                var actorService = c.Resolve<IActorSystemService>();
                actorService.Start(name, configFile);

                return actorService.Get;
            })
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerBuilder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddSigningActorProvider(this ContainerBuilder builder)
        {
            builder.RegisterType<SigningActorProvider>().As<ISigningActorProvider>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <param name="invoker"></param>
        /// <returns></returns>
        public static ContainerBuilder AddInterpretActorProvider<TAttach>(this ContainerBuilder builder, Func<IUnitOfWork, ISigningActorProvider, Props> invoker)
        {
            builder.Register(c =>
            {
                var interpretActorProvider = new InterpretActorProvider<TAttach>(
                    c.Resolve<IActorSystemService>(),
                    invoker,
                    c.Resolve<IUnitOfWork>(),
                    c.Resolve<ISigningActorProvider>(),
                    c.Resolve<ILogger<InterpretActorProvider<TAttach>>>());

                return interpretActorProvider;

            })
            .As<IInterpretActorProvider<TAttach>>()
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddProcessActorProvider<TAttach>(this ContainerBuilder builder)
        {
            builder.RegisterType<ProcessActorProvider<TAttach>>().As<IProcessActorProvider<TAttach>>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddGraphActorProvider<TAttach>(this ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var graphActorProvider = new GraphActorProvider<TAttach>
                (
                    c.Resolve<IActorSystemService>(),
                    c.Resolve<IUnitOfWork>(),
                    c.Resolve<IClusterProvider>(),
                    c.Resolve<IInterpretActorProvider<TAttach>>(),
                    c.Resolve<IProcessActorProvider<TAttach>>(),
                    c.Resolve<ISigningActorProvider>(),
                    c.Resolve<IJobActorProvider<TAttach>>()
                );

                return graphActorProvider;
            })
            .As<IGraphActorProvider<TAttach>>()
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddVerifiableFunctionsActorProvider(this ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var verifiableFunctionsActorProvider = new VerifiableFunctionsActorProvider
                (
                    c.Resolve<IActorSystemService>(),
                    c.Resolve<ISigningActorProvider>()
                );

                return verifiableFunctionsActorProvider;
            })
            .As<IVerifiableFunctionsActorProvider>()
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configFile"></param>
        /// <returns></returns>
        public static ContainerBuilder AddClusterProvider(this ContainerBuilder builder, string configFile)
        {
            builder.Register(c =>
            {
                var config = Helper.ConfigurationLoader.Load(configFile);
                var cluserProvider = new ClusterProvider
                (
                     c.Resolve<IActorSystemService>(),
                     config
                );

                return cluserProvider;
            })
            .As<IClusterProvider>()
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static ContainerBuilder AddPublisherBaseGraphProvider<TAttach>(this ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var publisher = new PublisherBaseGraphProvider<TAttach>
                (
                    c.Resolve<IActorSystemService>(),
                    c.Resolve<IUnitOfWork>(),
                    c.Resolve<IClusterProvider>(),
                    c.Resolve<ILogger<PublisherBaseGraphProvider<TAttach>>>()
                );

                return publisher;
            })
            .As<IPublisherBaseGraphProvider>()
            .SingleInstance();

            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static ContainerBuilder AddSubscriberProvider<TAttach>(this ContainerBuilder builder, string topic)
        {
            builder.Register(c =>
            {
                var subscriber = new SubscriberBaseGraphProvider<TAttach>
                (
                     c.Resolve<ActorSystem>(),
                     topic,
                     c.Resolve<Services.IBlockGraphService<TAttach>>(),
                     c.Resolve<ILogger<SubscriberBaseGraphProvider<TAttach>>>()
                 );

                return subscriber;
            })
            .As<ISubProvider>()
            .SingleInstance();

            return builder;
        }
    }
}
