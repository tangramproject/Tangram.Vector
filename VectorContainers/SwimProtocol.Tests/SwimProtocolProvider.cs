using Newtonsoft.Json;
using SwimProtocol.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Xunit.Abstractions;

namespace SwimProtocol.Tests
{
    public class SwimProtocolProvider : ISwimProtocolProvider
    {
        public ITestOutputHelper _output;
        public ISwimNode Node { get; set; }
        public byte[] SecretKey { get; set; }

        public event ReceivedMessageEventHandler ReceivedMessage;

        public SwimProtocolProvider(ISwimNode node, ITestOutputHelper output)
        {
            _output = output;
            Node = node;
        }

        public void OnMessageReceived(ReceivedMessageEventArgs e)
        {
            ReceivedMessage(this, e);
        }

        public Task Listen()
        {
            return Task.Run(() =>
            {
                var publicKey = Core.API.Onion.Utilities.ConvertV3OnionHostnameToEd25518PublicKey(Node.Hostname);
                var shortPub = new byte[] { publicKey[0], publicKey[0] };
                var port = BitConverter.ToUInt16(shortPub);

                using (var server = new WebServer($"http://localhost:{port}"))
                {
                    server.RegisterModule(new WebApiModule());
                    server.Module<WebApiModule>().RegisterController((ctx) =>
                    {
                        Debug.WriteLine("Registering Controller");
                        return new SwimController(ctx, this, _output);
                    });

                    Debug.WriteLine($"Listening on port {port}");

                    server.RunAsync();

                    while (true) { };
                }
            });
        }

        public SignedSwimMessage SignMessage(MessageBase message)
        {
            var originPublicKey = Core.API.Onion.Utilities.ConvertV3OnionHostnameToEd25518PublicKey(Node.Hostname);
            var signedMessage = SignedSwimMessage.Create(message, originPublicKey, SecretKey);
            return signedMessage;
        }

        public void SendMessage(ISwimNode dest, CompositeMessage message)
        {
            Task.Run(async () =>
            {
                using (HttpClient client = new HttpClient())
                {
                    var destPublicKey = Core.API.Onion.Utilities.ConvertV3OnionHostnameToEd25518PublicKey(dest.Hostname);
                    var shortPub = new byte[] { destPublicKey[0], destPublicKey[0] };
                    var destPort = BitConverter.ToUInt16(shortPub);

                    client.BaseAddress = new Uri($"http://localhost:{destPort}");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    using (var request = new HttpRequestMessage(HttpMethod.Post, "api/messages"))
                    {
                        var content = JsonConvert.SerializeObject(message, Formatting.Indented);
                        var buffer = Encoding.UTF8.GetBytes(content);

                        request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                        try
                        {
                            Debug.WriteLine($"Sent: {content}");

                            using (var response = await client.SendAsync(request))
                            {
                                var stream = await response.Content.ReadAsStreamAsync();
                                /*
                                if (response.IsSuccessStatusCode)
                                {
                                    //var result = Util.DeserializeJsonFromStream<JObject>(stream);
                                    //return Task.FromResult(result).Result;
                                }

                                var contentResult = await Util.StreamToStringAsync(stream);
                                throw new ApiException
                                {
                                    StatusCode = (int)response.StatusCode,
                                    Content = contentResult
                                };
                                */
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
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
    }
}
