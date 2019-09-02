using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Core.API.POS.Tests
{
    public class LotteryTicketTests
    {
        private readonly ITestOutputHelper output;

        public LotteryTicketTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task TestValidTarget()
        {
            output.WriteLine($"Target: {LotteryTicket.PoWTarget.ToString(10)}");

            var ticket = LotteryTicket.GenerateValidTarget(0);
            var hash = LotteryTicket.Hash(ticket);

            output.WriteLine($"Target Hit: {hash.ToString(10)}");

            output.WriteLine($"{JsonConvert.SerializeObject(ticket, Formatting.Indented)}");
        }
    }
}
