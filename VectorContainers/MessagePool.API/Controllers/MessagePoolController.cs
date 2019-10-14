using System.Net;
using System.Threading.Tasks;
using MessagePool.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessagePool.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagePoolController : Controller
    {
        readonly IMessagePoolService messagePoolService;

        public MessagePoolController(IMessagePoolService messagePoolService)
        {
            this.messagePoolService = messagePoolService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost(Name = "AddMessage")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddMessage([FromBody]byte[] message)
        {
            var msg = await messagePoolService.AddMessage(message);
            return new ObjectResult(new { protobuf = msg });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("count/{key}", Name = "Count")]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Count(string key)
        {
            var count = await messagePoolService.Count(key);
            return new ObjectResult(new { count });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet("messages/{key}/{skip}/{take}", Name = "GetMessagesByKey")]
        [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMessagesByKey(string key, int skip, int take)
        {
            var msgs = await messagePoolService.GetMessages(key, skip, take);
            return new ObjectResult(new { protobufs = msgs });
        }
    }
}
