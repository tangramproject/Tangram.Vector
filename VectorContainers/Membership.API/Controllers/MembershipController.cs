using Core.API.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SwimProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Membership.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipController : Controller
    {
        private ISwimProtocolProvider _swimProtocolProvider;
        private ISwimProtocol _swimProtocol;

        public MembershipController(ISwimProtocolProvider swimProtocolProvider, IHostedService swimProtocol)
        {
            _swimProtocolProvider = swimProtocolProvider;
            _swimProtocol = (ISwimProtocol)swimProtocol;
        }

        [HttpPost("messages", Name = "AddMessage")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> AddMessage([FromBody]CompositeMessage compositeMessage)
        {
            _swimProtocolProvider.OnMessageReceived(new ReceivedMessageEventArgs() { CompositeMessage = compositeMessage });
            return Accepted();
        }

        [HttpGet("members", Name = "Get Members")]
        [ProducesResponseType(typeof(List<INode>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<INode>> GetMembers()
        {
            return _swimProtocol.Members.Select(x => new Node
            {
                Endpoint = x.Endpoint
            });
        }
    }
}
