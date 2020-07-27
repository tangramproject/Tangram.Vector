// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using TGMNode.Services;
using TGMNode.Model;

namespace TGMNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger _logger;

        public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        [HttpPost("mempool", Name = "AddTransaction")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddTransaction([FromBody] byte[] tx)
        {
            try
            {
                var txProto = TGMCore.Helper.Util.DeserializeProto<TransactionProto>(tx);
                var txByteArray = await _transactionService.AddTransaction(txProto);

                return new ObjectResult(new { protobuf = txByteArray });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< AddTransaction - Controller >>> {ex}");
            }

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        [HttpGet("{hash}", Name = "GetTransaction")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransaction(string hash)
        {
            try
            {
                var tx = await _transactionService.GetTransaction(hash);
                return new ObjectResult(new { protobufs = tx });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< GetTransaction - Controller >>> {ex}");
            }

            return NotFound();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet("coins/{skip}/{take}", Name = "GetTransactions")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(int skip, int take)
        {
            try
            {
                var txs = await _transactionService.GetTransactions(skip, take);
                return new ObjectResult(new { protobufs = txs });
            }
            catch (Exception ex)
            {
                _logger.LogError($"<<< GetTransactions - Controller >>> {ex}");
            }

            return NotFound();
        }
    }
}
