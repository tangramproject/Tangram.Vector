﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coin.API.Model;
using Coin.API.Services;
using Core.API.Actors.Providers;
using Core.API.Helper;
using Core.API.Model;
using Core.API.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Coin.API.Controllers
{
    [Route("api/[controller]")]
    public class BlockGraphController : Controller
    {
        private readonly IBlockGraphService blockGraphService;
        private readonly IHttpClientService httpClientService;
        private readonly INetworkActorProvider networkProvider;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger logger;

        public BlockGraphController(IBlockGraphService blockGraphService, IHttpClientService httpClientService,
            INetworkActorProvider networkProvider, IUnitOfWork unitOfWork, ILogger<BlockGraphController> logger)
        {
            this.blockGraphService = blockGraphService;
            this.httpClientService = httpClientService;
            this.networkProvider = networkProvider;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraph"></param>
        /// <returns></returns>
        [HttpPost("blockgraph", Name = "AddBlock")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddBlock([FromBody]byte[] blockGraph)
        {
            try
            {
                var blockGrpahProto = Util.DeserializeProto<BaseGraphProto<CoinProto>>(blockGraph);
                var block = await blockGraphService.SetBlockGraph(blockGrpahProto);

                return new ObjectResult(new { protobuf = Util.SerializeProto(block) });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< AddBlock - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphs"></param>
        /// <returns></returns>
        [HttpPost("blockgraphs", Name = "AddBlocks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddBlocks([FromBody]byte[] blockGraphs)
        {
            try
            {
                var blockGraphProtos = Util.DeserializeListProto<BaseGraphProto<CoinProto>>(blockGraphs);
                if (blockGraphProtos?.Any() == true)
                {
                    var blockInfos = new List<BlockInfoProto>();

                    for (int i = 0; i < blockGraphProtos.Count(); i++)
                    {
                        var added = await blockGraphService.SetBlockGraph(blockGraphProtos.ElementAt(i));
                        if (added != null)
                        {
                            var next = blockGraphProtos.ElementAt(i);
                            blockInfos.Add(new BlockInfoProto { Hash = next.Block.Hash, Node = next.Block.Node, Round = next.Block.Round });
                        }
                    }

                    return new ObjectResult(new { protobufs = Util.SerializeProto(blockInfos) });
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< AddBlocks - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("height", Name = "BlockHeight")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BlockHeight()
        {
            try
            {
                var blockHeight = await networkProvider.BlockHeight();
                return new ObjectResult(new { height = blockHeight });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockHeight - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("networkheight", Name = "NetworkBlockHeight")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> NetworkBlockHeight()
        {
            try
            {
                var blockHeight = await networkProvider.NetworkBlockHeight();
                return new ObjectResult(new { height = blockHeight });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< NetworkBlockHeight - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        [HttpGet("mempool/{hash}/{round}", Name = "MemPoolBlockGraph")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MemPoolBlockGraph(string hash, int round)
        {
            try
            {
                //var blockGraph = await unitOfWork.BlockGraph
                //    .GetWhere(x => x.Block.Hash.Equals(hash) && x.Block.Node.Equals(httpClientService.NodeIdentity) && x.Block.Round.Equals(round));

                return new ObjectResult(new { protobuf = Util.SerializeProto(new byte()) });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MemPoolBlockGraph - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("identity", Name = "Identity")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Identity()
        {
            try
            {
                var pubKey = Request.Headers["x-pub"];
                var peer = Util.HashToId(pubKey.First());
                var identity = httpClientService.GetIdentity(peer);
                var signedPayload = await httpClientService.SignPayload(identity);

                return new ObjectResult(new { identity = Util.SerializeProto(signedPayload) });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Identity - Controller >>>: {ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
