using System;
using System.Threading.Tasks;
using Core.API.MQTT;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Serilog;
using Serilog.Events;

namespace Broker.API.Node
{
    public class RemoteNode : INode
    {
        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly ClientStorageManager clientStorageManager;

        private IManagedMqttClient client;

        public RemoteNode(ulong id, string host, int port)
        {
            this.id = id;
            this.host = host;
            this.port = port;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("MQTT.Node.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2)
                .CreateLogger();

            clientStorageManager = new ClientStorageManager(@$"RetainedMessages-{id}.json");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{id} host={host}:{port}";
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task<bool> Start()
        {
            Log.Information($"Starting replication client {this}...");

            client = new MqttFactory().CreateManagedMqttClient();
            client.UseConnectedHandler(args => Log.Information($"Replication client connected {this}"));
            client.UseDisconnectedHandler(args => Log.Information($"Replication client disconnected {this}"));
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithStorage(clientStorageManager)
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"Replication-{id}")
                    .WithTcpServer(host, port)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(20))
                    .WithKeepAliveSendInterval(TimeSpan.FromSeconds(10))
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(5))
                    .Build())
                .Build();

            await client.StartAsync(options);

            return client.IsStarted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public async Task ReplicateMessage(MqttApplicationMessage message)
        {
            var replicated = new MqttApplicationMessageBuilder()
                    .WithTopic($"{Extentions.MqttApplicationMessageExtensions.ReplicationTopic}{message.Topic}")
                    .WithPayload(message.Payload)
                    .WithQualityOfServiceLevel(message.QualityOfServiceLevel)
                    .WithContentType(message.ContentType)
                    .WithCorrelationData(message.CorrelationData)
                    .WithResponseTopic(message.ResponseTopic)
                    .WithRetainFlag(message.Retain);

            if (message.MessageExpiryInterval.HasValue)
            {
                replicated.WithMessageExpiryInterval(message.MessageExpiryInterval.Value);
            }

            if (message.PayloadFormatIndicator.HasValue)
            {
                replicated.WithPayloadFormatIndicator(message.PayloadFormatIndicator.Value);
            }

            if (message.TopicAlias.HasValue)
            {
                replicated.WithTopicAlias(message.TopicAlias.Value);
            }

            if (message.SubscriptionIdentifiers != null)
            {
                foreach (var identifier in message.SubscriptionIdentifiers)
                {
                    replicated.WithSubscriptionIdentifier(identifier);
                }
            }

            var result = await client.PublishAsync(replicated.Build());

            Log.Information($"Replicating message to {this}... {result.ReasonCode}");
        }
    }
}
