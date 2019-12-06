using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;
using Microsoft.Extensions.Logging;

namespace Core.API.Actors.Providers
{
    public class NetworkActorProvider<TAttach> : INetworkActorProvider
    {
        private readonly IActorRef actor;
        private readonly ILogger logger;

        public NetworkActorProvider(ActorSystem actorSystem, IUnitOfWork unitOfWork, IHttpClientService httpClientService, ILogger<NetworkActorProvider<TAttach>> logger)
        {
            this.logger = logger;

            var actorProps = NetworkActor<TAttach>.Create(unitOfWork, httpClientService).WithRouter(new RoundRobinPool(5));
            actor = actorSystem.ActorOf(actorProps, "network-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<int> BlockHeight()
        {
            int result = 0;

            try
            {
                result = await actor.Ask<int>(new BlockHeightMessage(), new TimeSpan(0, 0, 5));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkActorProvider.BlockHeight >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<NodeBlockCountProto>> FullNetworkBlockHeight()
        {
            var nodeBlockCounts = Enumerable.Empty<NodeBlockCountProto>();

            try
            {
                nodeBlockCounts = await actor.Ask<IEnumerable<NodeBlockCountProto>>(new FullNetworkBlockHeightMessage(), new TimeSpan(0, 0, 30));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkActorProvider.FullNetworkBlockHeight >>>: {ex.ToString()}");
            }

            return nodeBlockCounts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> NetworkBlockHeight()
        {
            ulong result = 0;

            try
            {
                result = await actor.Ask<ulong>(new NetworkBlockHeightMessage(), new TimeSpan(0, 0, 30));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkActorProvider.NetworkBlockHeight >>>: {ex.ToString()}");
            }

            return result;
        }
    }
}
