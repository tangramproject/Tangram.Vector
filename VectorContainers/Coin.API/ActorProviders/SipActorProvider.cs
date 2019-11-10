using System;
using System.Threading.Tasks;
using Akka.Actor;
using Coin.API.Actors;
using Coin.API.Services;
using Core.API.Messages;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Coin.API.ActorProviders
{
    public class SipActorProvider : ISipActorProvider
    {
        private readonly IActorRef actor;
        private readonly ILogger logger;

        public SipActorProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork, IHttpService httpService, IInterpretActorProvider interpretActorProvider,
            IProcessBlockActorProvider processBlockActorProvider, ISigningActorProvider signingActorProvider, ILogger<SipActorProvider> logger)
        {
            this.logger = logger;

            actor = actorSystem.ActorOf(SipActor.Props(unitOfWork, httpService, interpretActorProvider, processBlockActorProvider, signingActorProvider), "sip-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messge"></param>
        /// <returns></returns>
        public async Task<bool> GracefulStop(GracefulStopMessge messge)
        {
            if (messge == null)
                throw new ArgumentNullException(nameof(messge));

            if (messge.Hash == null)
                throw new ArgumentNullException(nameof(messge.Hash));

            if (messge.Hash.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(messge.Hash));

            if (string.IsNullOrEmpty(messge.Reason))
                throw new ArgumentNullException(nameof(messge.Reason));

            if (messge.TimeSpan == TimeSpan.Zero)
                throw new ArgumentNullException(nameof(messge.Hash));

            bool result = false;

            try
            {
                result = await actor.Ask<bool>(messge);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< SupervisorActorProvider.GracefulStop >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Register(HashedMessage message)
        {
            actor.Tell(message);
        }
    }
}
