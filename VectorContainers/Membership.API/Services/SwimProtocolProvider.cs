using Core.API.Onion;
using Newtonsoft.Json;
using SwimProtocol;
using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.API.LibSodium;
using Microsoft.Extensions.Logging;

namespace Membership.API.Services
{
    public class SwimProtocolProvider : ISwimProtocolProvider
    {
        public ISwimNode Node { get; set; }

        public event ReceivedMessageEventHandler ReceivedMessage;

        private readonly ITorClient _torClient;
        private readonly IOnionServiceClient _onionServiceClient;
        private readonly ILogger _logger;

        public SwimProtocolProvider(ITorClient torClient, IOnionServiceClient onionServiceClient, ISwimNode node, ILogger<SwimProtocolProvider> logger)
        {
            Node = node;
            _torClient = torClient;
            _onionServiceClient = onionServiceClient;
            _logger = logger;
        }

        public void OnMessageReceived(ReceivedMessageEventArgs e)
        {
            ReceivedMessage?.Invoke(this, e);
        }

        public void SendMessage(ISwimNode dest, CompositeMessage message)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                var uri = new Uri(new Uri(dest.Endpoint), "membership/messages");
                _ = await _torClient.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"), new System.Threading.CancellationToken());
            });
        }

        public void SendMessage(ISwimNode dest, IEnumerable<SignedSwimMessage> messages)
        {
            var compositeMessage = new CompositeMessage(messages);
            SendMessage(dest, compositeMessage);
        }

        public void SendMessage(ISwimNode dest, MessageBase message)
        {
            var signedMessage = SignMessage(message);
            SendMessage(dest, new List<SignedSwimMessage> { signedMessage });
        }

        public SignedSwimMessage SignMessage(MessageBase message)
        {
            var serialized = JsonConvert.SerializeObject(message);
            var hash = Cryptography.GenericHashNoKey(serialized);

            var signedHashResponse = _onionServiceClient.SignHashAsync(hash)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return new SignedSwimMessage
            {
                Hash = hash,
                Message = message,
                Signature = signedHashResponse.Signature,
                PublicKey = signedHashResponse.PublicKey
            };
        }
    }
}
