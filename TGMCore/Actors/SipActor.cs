// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Providers;
using TGMCore.Extentions;
using TGMCore.Messages;
using TGMCore.Model;

namespace TGMCore.Actors
{
    public class SipActor<TAttach> : ReceiveActor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClusterProvider _clusterProvider;
        private readonly IInterpretActorProvider<TAttach> _interpretActorProvider;
        private readonly IProcessActorProvider<TAttach> _processActorProvider;
        private readonly ISigningActorProvider _signingActorProvider;
        private readonly IPubProvider _pubProvider;
        private readonly ILoggingAdapter _logger;

        protected Dictionary<string, IActorRef> BoostGraphs;

        public SipActor(IUnitOfWork unitOfWork, IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
           IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider, IPubProvider pubProvider)
        {
            _unitOfWork = unitOfWork;
            _clusterProvider = clusterProvider;
            _interpretActorProvider = interpretActorProvider;
            _processActorProvider = processActorProvider;
            _signingActorProvider = signingActorProvider;
            _pubProvider = pubProvider;

            _logger = Context.GetLogger();

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

            if (message.Hash.Length != 33)
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

            _logger.Warning(message.Reason);

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
                //var boostGraphActorProps = GraphActor<TAttach>.Create(_unitOfWork, _clusterProvider, _interpretActorProvider, _processActorProvider, _signingActorProvider, _pubProvider);
                //var @ref = Context.ActorOf(boostGraphActorProps, name);

                //BoostGraphs.TryAdd(hash.ToHex(), @ref);

                //return @ref;
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
        public static Props Create(IUnitOfWork unitOfWork, IClusterProvider clusterProvider, IInterpretActorProvider<TAttach> interpretActorProvider,
            IProcessActorProvider<TAttach> processActorProvider, ISigningActorProvider signingActorProvider, IPubProvider pubProvider) =>
            Props.Create(() => new SipActor<TAttach>(unitOfWork, clusterProvider, interpretActorProvider, processActorProvider, signingActorProvider, pubProvider));
    }
}
