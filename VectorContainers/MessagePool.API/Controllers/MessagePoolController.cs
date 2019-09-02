using System.Net;
using System.Threading.Tasks;
using Core.API.Model;
using MessagePool.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System;

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

        [HttpPost(Name = "AddMessage")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddMessage([FromBody]MessageProto messageProto)
        {
            messageProto.Address = Encoding.UTF8.GetString(Convert.FromBase64String(messageProto.Address));
            messageProto.Body = Encoding.UTF8.GetString(Convert.FromBase64String(messageProto.Body));

            var msg = await messagePoolService.AddMessage(messageProto);
            return Ok(msg);
        }

        [HttpGet("count/{address}", Name = "Count")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Count(string address)
        {
            var count = await messagePoolService.Count(address);
            dynamic dynamic = new { count };

            return Ok(dynamic);
        }

        [HttpGet("messages/{key}/{skip}/{take}", Name = "GetMessagesByKey")]
        [ProducesResponseType(typeof(List<Message>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMessagesByKey(string key, int skip, int take)
        {
            var messages = await messagePoolService.GetMessages(key, skip, take);
            return Ok(messages);
        }
    }
}
