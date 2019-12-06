using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Coin.API.Services;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using Coin.API.Model;

namespace Coin.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoinController : Controller
    {
        private readonly ICoinService coinService;
        private readonly ILogger logger;

        public CoinController(ICoinService coinService, ILogger<CoinController> logger)
        {
            this.coinService = coinService;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        [HttpPost("mempool", Name = "AddCoin")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddCoin([FromBody] byte[] coin)
        {
            try
            {
                var coinProto = Util.DeserializeProto<CoinProto>(coin);
                var coinByteArray = await coinService.AddCoin(coinProto);

                return new ObjectResult(new { protobuf = coinByteArray });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< AddCoin - Controller >>>{ex.ToString()}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        [HttpGet("{hash}", Name = "GetCoin")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoin(string hash)
        {
            try
            {
                var coin = await coinService.GetCoin(hash);
                return new ObjectResult(new { protobufs = coin });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< GetCoin - Controller >>>{ex.ToString()}");
            }

            return NotFound();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet("coins/{skip}/{take}", Name = "GetCoins")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCoins(int skip, int take)
        {
            try
            {
                var coins = await coinService.GetCoins(skip, take);
                return new ObjectResult(new { protobufs = coins });
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< GetCoins - Controller >>>{ex.ToString()}");
            }

            return NotFound();
        }
    }
}
