using System.Net;
using System.Threading.Tasks;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.AspNetCore.Mvc;

namespace Coin.API.Controllers
{
    [Route("api/[controller]")]
    public class BlockGraphController : Controller
    {
        private readonly IBlockGraphService blockGraphService;

        public BlockGraphController(IBlockGraphService blockGraphService)
        {
            this.blockGraphService = blockGraphService;
        }

        [HttpPost(Name = "AddBlock")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddBlock([FromBody]BlockGraphProto blockProto)
        {
            var proto = await blockGraphService.AddBlockGraph(blockProto);
            return new ObjectResult(new { protobuf = Util.SerializeProto(proto) });
        }

        [HttpGet("blockheight", Name = "BlockHeight")]
        [ProducesResponseType(typeof(long), (int)HttpStatusCode.OK)]
        public IActionResult BlockHeight()
        {
            var blockHeight = blockGraphService.BlockHeight();
            return new ObjectResult(new { height = blockHeight });
        }
    }
}
