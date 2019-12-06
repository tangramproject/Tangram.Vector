using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Network;
using Newtonsoft.Json.Linq;

namespace Core.API.Actors
{
    public class NetworkActor<TAttach> : ReceiveActor
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpClientService httpClientService;
        private readonly ILoggingAdapter logger;
        private readonly IBaseBlockIDRepository<TAttach> baseBlockIDRepository;

        public NetworkActor(IUnitOfWork unitOfWork, IHttpClientService httpClientService)
        {
            this.unitOfWork = unitOfWork;
            this.httpClientService = httpClientService;

            logger = Context.GetLogger();
            baseBlockIDRepository = unitOfWork.CreateBaseBlockIDOf<TAttach>();

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
                var responses = await httpClientService.Dial(DialType.Get, "height");
                foreach (var response in responses)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var fullNodeIdentity = httpClientService.GetFullNodeIdentity(response);

                        var jToken = Helper.Util.ReadJToken(response, "height");
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
                height = await baseBlockIDRepository.Count(httpClientService.NodeIdentity);
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
        /// <param name="httpClientService"></param>
        /// <returns></returns>
        public static Props Create(IUnitOfWork unitOfWork, IHttpClientService httpClientService) =>
            Props.Create(() => new NetworkActor<TAttach>(unitOfWork, httpClientService));
    }
}
