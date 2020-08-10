// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using Akka.Actor;
using TGMCore.Providers;
using TGMCore.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Akka.Configuration;
using Autofac;
using Akka.DI.AutoFac;

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
            var config = Helper.ConfigurationLoader.Load(configFile).WithFallback(ConfigurationFactory.Default());
            builder.Register(c =>
            {
                var actorSystem = ActorSystem.Create(name, config);
                return actorSystem;
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
                    c.Resolve<ActorSystem>(),
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
        /// <typeparam name="Startup"></typeparam>
        /// <typeparam name="TAttach"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddSipActorProvider<Startup, TAttach>(this ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var sipActorProvider = new SipActorProvider<TAttach>
                (
                    c.Resolve<ActorSystem>(),
                    c.Resolve<IUnitOfWork>(),
                    c.Resolve<IClusterProvider>(),
                    c.Resolve<IInterpretActorProvider<TAttach>>(),
                    c.Resolve<IProcessActorProvider<TAttach>>(),
                    c.Resolve<ISigningActorProvider>(),
                    c.Resolve<IPubProvider>(),
                    c.Resolve<ILogger<SipActorProvider<TAttach>>>()
                );

                return sipActorProvider;
            })
            .As<ISipActorProvider>()
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
                    c.Resolve<ActorSystem>(),
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
                     c.Resolve<ActorSystem>(),
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
        public static ContainerBuilder AddPublisherProvider<TAttach>(this ContainerBuilder builder, string topic = null)
        {
            builder.Register(c =>
            {
                var publisher = new PublisherBaseGraphProvider<TAttach>
                (
                    c.Resolve<ActorSystem>(),
                    c.Resolve<IUnitOfWork>(),
                    c.Resolve<IClusterProvider>(),
                    c.Resolve<ILogger<PublisherBaseGraphProvider<TAttach>>>(),
                    topic
                );

                return publisher;
            })
            .As<IPubProvider>()
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
