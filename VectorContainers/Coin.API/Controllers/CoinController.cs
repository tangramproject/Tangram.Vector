using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Coin.API.Services;

namespace Coin.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoinController : Controller
    {
        readonly ICoinService coinService;

        public CoinController(ICoinService coinService)
        {
            this.coinService = coinService;
        }

        [HttpPost("mempool", Name = "AddCoin")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddCoin([FromBody] byte[] coin)
        {
            var cn = await coinService.AddCoin(coin);
            return new ObjectResult(new { protobuf = cn });
        }

        [HttpGet("{address}", Name = "GetCoin")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCoin(byte[] address)
        {
            var coin = await coinService.GetCoin(address);
            return new ObjectResult(new { protobuf = coin });
        }
    }
}
