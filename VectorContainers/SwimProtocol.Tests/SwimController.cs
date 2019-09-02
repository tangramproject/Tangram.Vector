using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Xunit.Abstractions;

namespace SwimProtocol.Tests
{
    public class SwimController : WebApiController
    {
        private ISwimProtocolProvider _swimProtocolProvider;
        private IHttpContext _context;
        private readonly ITestOutputHelper _output;

        public SwimController(IHttpContext context, ISwimProtocolProvider swimProtocolProvider, ITestOutputHelper output) : base(context)
        {
            _swimProtocolProvider = swimProtocolProvider;
            _context = context;
            _output = output;
        }

        [WebApiHandler(HttpVerbs.Post, "/api/messages")]
        public async Task<bool> AddMessage()
        {
            var body = await _context.RequestBodyAsync();

            var compositeMessage = JsonConvert.DeserializeObject<CompositeMessage>(body);

            _swimProtocolProvider.OnMessageReceived(new ReceivedMessageEventArgs() { CompositeMessage = compositeMessage });

            return await this.JsonResponseAsync(new { status = "SUCCESS" });
        }

        [WebApiHandler(HttpVerbs.Get, "/")]
        public async Task<bool> TestAsync(MessageBase message)
        {
            return await this.JsonResponseAsync(new { status = "SUCCESS" });
        }
    }
}
