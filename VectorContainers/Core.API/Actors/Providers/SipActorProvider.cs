using System;
using System.Threading.Tasks;
using Akka.Actor;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;
using Microsoft.Extensions.Logging;

namespace Core.API.Actors.Providers
{
    public class SipActorProvider<TAttach> : ISipActorProvider
    {

        private readonly IActorRef actor;
        private readonly ILogger logger;

        public SipActorProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork, IHttpClientService httpClientService, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider, ILogger<SipActorProvider<TAttach>> logger)
        {
            this.logger = logger;
            var sipActorProps = SipActor<TAttach>.Create(unitOfWork, httpClientService, interpretActorProvider, processActorProvider, signingActorProvider);

            actor = actorSystem.ActorOf(sipActorProps, "sip-actor");
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
