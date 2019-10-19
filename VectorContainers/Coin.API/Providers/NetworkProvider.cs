using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Coin.API.Providers
{
    public class NetworkProvider
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IHttpService httpService;
        private readonly ILogger logger;

        public NetworkProvider(IUnitOfWork unitOfWork, IHttpService httpService, ILogger<NetworkProvider> logger)
        {
            this.unitOfWork = unitOfWork;
            this.httpService = httpService;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<long> NetworkBlockHeight()
        {
            long height = 0;

            try
            {
                var list = await FullNetworkBlockHeight();
                if (list.Any())
                {
                    height = list.Max(m => m.BlockCount);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkProvider.NetworkBlockHeight >>>: {ex.ToString()}");
            }

            return height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<NodeBlockCountProto>> FullNetworkBlockHeight()
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
                        list.Add(new NodeBlockCountProto { Address = fullNodeIdentity.Value, BlockCount = jToken.Value<long>(), Node = fullNodeIdentity.Key });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkProvider.FullNetworkBlockHeight >>>: {ex.ToString()}");
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<int> BlockHeight()
        {
            int blockHeight = 0;

            try
            {
                blockHeight = await unitOfWork.BlockID.Count(httpService.NodeIdentity);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkProvider.BlockHeight >>>: {ex.ToString()}");
            }

            return blockHeight;
        }
    }
}
