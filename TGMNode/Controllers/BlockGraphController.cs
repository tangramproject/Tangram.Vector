// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TGMCore.Model;
using TGMCore.Services;
using TGMNode.Model;

namespace TGMNode.Controllers
{
    [Route("api/[controller]")]
    public class BlockGraphController : Controller
    {
        private readonly IBlockGraphService<TransactionProto> _blockGraphService;
        private readonly ILogger _logger;

        public BlockGraphController(IBlockGraphService<TransactionProto> blockGraphService, ILogger<BlockGraphController> logger)
        {
            _blockGraphService = blockGraphService;
            _logger = logger;
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
                var blockGrpahProto = TGMCore.Helper.Util.DeserializeProto<BaseGraphProto<TransactionProto>>(blockGraph);
                var block = await _blockGraphService.SetBlockGraph(blockGrpahProto);

                return new ObjectResult(new { protobuf = TGMCore.Helper.Util.SerializeProto(block) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< AddBlock - Controller >>>: {ex}");
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
                var blockGraphProtos = TGMCore.Helper.Util.DeserializeListProto<BaseGraphProto<TransactionProto>>(blockGraphs);
                if (blockGraphProtos?.Any() == true)
                {
                    var blockInfos = new List<BlockInfoProto>();

                    for (int i = 0; i < blockGraphProtos.Count(); i++)
                    {
                        var added = await _blockGraphService.SetBlockGraph(blockGraphProtos.ElementAt(i));
                        if (added != null)
                        {
                            var next = blockGraphProtos.ElementAt(i);
                            blockInfos.Add(new BlockInfoProto { Hash = next.Block.Hash, Node = next.Block.Node, Round = next.Block.Round });
                        }
                    }

                    return new ObjectResult(new { protobufs = TGMCore.Helper.Util.SerializeProto(blockInfos) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< AddBlocks - Controller >>>: {ex}");
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
                var blockHeight = 0; //await _networkProvider.BlockHeight();
                return new ObjectResult(new { height = blockHeight });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< BlockHeight - Controller >>>: {ex}");
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
                var blockHeight = 0; // await _networkProvider.NetworkBlockHeight();
                return new ObjectResult(new { height = blockHeight });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< NetworkBlockHeight - Controller >>>: {ex}");
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

                return new ObjectResult(new { protobuf = TGMCore.Helper.Util.SerializeProto(new byte()) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< MemPoolBlockGraph - Controller >>>: {ex}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
