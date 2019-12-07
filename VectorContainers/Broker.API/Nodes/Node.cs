using System;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Broker.API.Nodes
{
    public class Node: INode
    {
        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly ILogger logger;
        
        private IManagedMqttClient client;

        public Node(ulong id, string host, int port)
        {
            this.id = id;
            this.host = host;
            this.port = port;

            var loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<Node>();
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
        public void Start()
        {
            logger.LogInformation($"Starting replication client {this}...");

            client = new MqttFactory().CreateManagedMqttClient();
            client.UseConnectedHandler(args => logger.LogInformation($"Replication client connected {this}"));
            client.UseDisconnectedHandler(args => logger.LogInformation($"Replication client disconnected {this}"));
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"Replication-{id}")
                    .WithTcpServer(host, port)
                    .Build())
                .Build();

            client.StartAsync(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void ReplicateMessage(MqttApplicationMessage message)
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

            var result = client.PublishAsync(replicated.Build()).Result;

            logger.LogInformation($"Replicating message to {this}... {result.ReasonCode}");
        }
    }
}
