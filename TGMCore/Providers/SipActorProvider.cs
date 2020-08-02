// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading.Tasks;
using Akka.Actor;
using TGMCore.Messages;
using TGMCore.Model;
using Microsoft.Extensions.Logging;
using TGMCore.Actors;

namespace TGMCore.Providers
{
    public class SipActorProvider<TAttach> : ISipActorProvider
    {

        private readonly IActorRef _actor;
        private readonly ILogger _logger;

        public SipActorProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork,
            IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider,
            IPublisherBaseGraphProvider publisherBaseGraphProvider, ILogger<SipActorProvider<TAttach>> logger)
        {
            _logger = logger;

            var sipActorProps = SipActor<TAttach>
                .Create(unitOfWork, clusterProvider, interpretActorProvider, processActorProvider, signingActorProvider, publisherBaseGraphProvider);

            _actor = actorSystem.ActorOf(sipActorProps, "sip-actor");
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
                result = await _actor.Ask<bool>(messge);
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< SupervisorActorProvider.GracefulStop >>>: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void Register(HashedMessage message)
        {
            _actor.Tell(message);
        }
    }
}
