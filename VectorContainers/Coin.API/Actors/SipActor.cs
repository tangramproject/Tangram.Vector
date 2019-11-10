using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.ActorProviders;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;

namespace Coin.API.Actors
{
    public class SipActor : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly IInterpretActorProvider interpretActorProvider;
        private readonly IProcessBlockActorProvider processBlockActorProvider;
        private readonly ISigningActorProvider signingActorProvider;
        private readonly ILoggingAdapter logger;

        protected Dictionary<string, IActorRef> BoostGraphs;

        public SipActor(IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
           IProcessBlockActorProvider processBlockActorProvider, ISigningActorProvider signingActorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.interpretActorProvider = interpretActorProvider;
            this.processBlockActorProvider = processBlockActorProvider;
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
                var name = $"boostgraph-actor-{Util.HashToId(hash.ToHex())}";
                var @ref = Context.ActorOf(BoostGraphActor.Props(unitOfWork, httpService, interpretActorProvider, processBlockActorProvider, signingActorProvider), name);

                BoostGraphs.TryAdd(hash.ToHex(), @ref);

                return @ref;
            }

            return actorRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="httpService"></param>
        /// <param name="interpretActorProvider"></param>
        /// <param name="processBlockActorProvider"></param>
        /// <param name="signingActorProvider"></param>
        /// <returns></returns>
        public static Props Props(IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
            IProcessBlockActorProvider processBlockActorProvider, ISigningActorProvider signingActorProvider) =>
            Akka.Actor.Props.Create(() => new SipActor(unitOfWork, httpService, interpretActorProvider, processBlockActorProvider, signingActorProvider));
    }
}
