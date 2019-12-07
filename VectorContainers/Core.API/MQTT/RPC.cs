using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Protocol;

namespace Core.API.MQTT
{
    public class RPC
    {
        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly ILogger logger;
        private readonly IMqttClient client;
        private MqttRpcClient rpcClient;

        public RPC(ulong id, string host, int port)
        {
            this.id = id;
            this.host = host;
            this.port = port;

            var loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<RPC>();

            client = new MqttFactory().CreateMqttClient();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IMqttClient> Connect()
        {
            try
            {
                var options = new MqttClientOptionsBuilder();
                await client.ConnectAsync(options.WithClientId($"RPC-{id}").WithTcpServer(host, port).Build());

                if (client.IsConnected)
                {
                    rpcClient = new MqttRpcClient(client, new MqttRpcClientOptionsBuilder().Build());
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< RPC.Connect >>>: {ex.ToString()}");
            }

            return client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <param name="qos"></param>
        /// <returns></returns>
        public async Task<byte[]> Send(TimeSpan timeout, string topic, string payload, MqttQualityOfServiceLevel qos)
        {
            if (rpcClient == null)
                throw new NullReferenceException(nameof(rpcClient));

            return await rpcClient.ExecuteAsync(timeout, topic, payload, qos);
        }
    }
}
