using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using Newtonsoft.Json.Linq;

namespace Coin.API.Actors
{
    public class NetworkActor : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ILoggingAdapter logger;

        public NetworkActor(IUnitOfWork unitOfWork, IHttpService httpService)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;

            logger = Context.GetLogger();

            ReceiveAsync<BlockHeightMessage>(async msg => Sender.Tell(await BlockHeight()));
            ReceiveAsync<FullNetworkBlockHeightMessage>(async msg => Sender.Tell(await FullNetworkBlockHeight()));
            ReceiveAsync<NetworkBlockHeightMessage>(async msg => Sender.Tell(await NetworkBlockHeight()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<NetworkBlockHeightMessage> NetworkBlockHeight()
        {
            ulong height = 0;

            try
            {
                var list = await FullNetworkBlockHeight();
                if (list.NodeBlockCounts.Any())
                {
                    height = list.NodeBlockCounts.Max(m => m.BlockCount);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< NetworkProvider.NetworkBlockHeight >>>: {ex.ToString()}");
            }

            return new NetworkBlockHeightMessage { Height = height };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<FullNetworkBlockHeightMessage> FullNetworkBlockHeight()
        {
            var list = new List<NodeBlockCountProto>();

            try
            {
                var responses = await httpService.Dial(DialType.Get, "height");
                foreach (var response in responses)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var fullNodeIdentity = httpService.GetFullNodeIdentity(response);

                        var jToken = Util.ReadJToken(response, "height");
                        list.Add(new NodeBlockCountProto { Address = fullNodeIdentity.Value, BlockCount = jToken.Value<ulong>(), Node = fullNodeIdentity.Key });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"<<< NetworkProvider.FullNetworkBlockHeight >>>: {ex.ToString()}");
            }

            return new FullNetworkBlockHeightMessage { NodeBlockCounts = list };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<int> BlockHeight()
        {
            int height = 0;

            try
            {
                height = await unitOfWork.BlockID.Count(httpService.NodeIdentity);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< NetworkProvider.BlockHeight >>>: {ex.ToString()}");
            }

            return height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="httpService"></param>
        /// <returns></returns>
        public static Props Props(IUnitOfWork unitOfWork, IHttpService httpService) =>
            Akka.Actor.Props.Create(() => new NetworkActor(unitOfWork, httpService));
    }
}
