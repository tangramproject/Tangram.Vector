using Core.API.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SwimProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Membership.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipController : Controller
    {
        private ISwimProtocolProvider _swimProtocolProvider;
        private FailureDetectionProvider _swimProtocol;
        private ILogger _logger;

        public MembershipController(ISwimProtocolProvider swimProtocolProvider, FailureDetectionProvider swimProtocol, ILogger<MembershipController> logger)
        {
            _swimProtocolProvider = swimProtocolProvider;
            _swimProtocol = swimProtocol;
            _logger = logger;
        }

        [HttpPost("messages", Name = "AddMessage")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> AddMessage([FromBody] CompositeMessage compositeMessage)
        {
            if (compositeMessage != null)
            {
                _swimProtocolProvider.OnMessageReceived(new ReceivedMessageEventArgs()
                { CompositeMessage = compositeMessage });
            }
            else
            {
                _logger.LogWarning("Received NULL compositeMessage... skipping");
            }

            return await Task.FromResult(Accepted());
        }

        [HttpGet("members", Name = "Get Members")]
        [ProducesResponseType(typeof(List<INode>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<INode>> GetMembers()
        {
            return await Task.FromResult(_swimProtocol.Members.Select(x => new Node
            {
                Endpoint = x.Endpoint
            }));
        }
    }
}
