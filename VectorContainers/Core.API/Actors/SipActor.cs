using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Actors.Providers;
using Core.API.Extentions;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;

namespace Core.API.Actors
{
    public class SipActor<TAttach> : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly IInterpretActorProvider<TAttach> interpretActorProvider;
        private readonly IProcessActorProvider<TAttach> processActorProvider;
        private readonly ISigningActorProvider signingActorProvider;
        private readonly ILoggingAdapter logger;

        protected Dictionary<string, IActorRef> BoostGraphs;

        public SipActor(IUnitOfWork unitOfWork, IHttpClientService httpClientService, IInterpretActorProvider<TAttach> interpretActorProvider,
           IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;
            this.interpretActorProvider = interpretActorProvider;
            this.processActorProvider = processActorProvider;
            this.signingActorProvider = signingActorProvider;

            logger = Context.GetLogger();

            BoostGraphs = new Dictionary<string, IActorRef>();

            Receive<HashedMessage>(Register);
            ReceiveAsync<GracefulStopMessge>(async message => await GracefulStop(message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void Register(HashedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            if (message.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(message.Hash));

            var boostGraph = Exists(message.Hash);
            if (boostGraph != null)
            {
                boostGraph.Tell(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<bool> GracefulStop(GracefulStopMessge message)
        {
            bool result = false;

            logger.Warning(message.Reason);

            if (BoostGraphs.TryGetValue(message.Hash.ToHex(), out IActorRef actorRef))
            {
                result = await actorRef.GracefulStop(message.TimeSpan);
                if (result)
                {
                    BoostGraphs.Remove(message.Hash.ToHex());
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private IActorRef Exists(byte[] hash)
        {
            if (!BoostGraphs.TryGetValue(hash.ToHex(), out IActorRef actorRef))
            {
                var name = $"graph-actor-{Helper.Util.HashToId(hash.ToHex())}";
                var boostGraphActorProps = GraphActor<TAttach>.Create(unitOfWork, httpClientService, interpretActorProvider, processActorProvider, signingActorProvider);
                var @ref = Context.ActorOf(boostGraphActorProps, name);

                BoostGraphs.TryAdd(hash.ToHex(), @ref);

                return @ref;
            }

            return actorRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="httpClientService"></param>
        /// <param name="interpretActorProvider"></param>
        /// <param name="processActorProvider"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IHttpClientService httpClientService, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider) =>
            Props.Create(() => new SipActor<TAttach>(unitOfWork, httpClientService, interpretActorProvider, processActorProvider, signingActorProvider));
    }
}
