using System;
using Xunit;
using FakeItEasy;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Swim.Tests
{
    public class SwimTests
    {
        [Fact]
        public async Task RoundRobinTest()
        {
            var fakeProvider = A.Fake<ISwimProtocolProvider>();
            var fakeLogger = A.Fake<ILogger>();

            var client = new SwimClient(fakeProvider, fakeLogger);

            var nodes = A.CollectionOfDummy<SwimNode>(10);

            Random rnd = new Random();

            //foreach (var node in nodes)
            //{
            //    node.Endpoint = rnd.Next().ToString();
            //    client.Nodes.Enqueue(node);
            //}

            await client.ProtocolLoop();
        }
    }
}
