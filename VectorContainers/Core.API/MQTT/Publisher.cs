using System;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Core.API.MQTT
{
    public class Publisher
    {
        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly ILogger logger;
        private readonly IManagedMqttClient client;

        public Publisher(ulong id, string host, int port)
        {
            this.id = id;
            this.host = host;
            this.port = port;

            var loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<Publisher>();

            client = new MqttFactory().CreateManagedMqttClient();

            Start();
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Start()
        {
            var options = new ManagedMqttClientOptionsBuilder()
              .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
              .WithClientOptions(new MqttClientOptionsBuilder()
                  .WithClientId($"Publisher-{id}")
                  .WithTcpServer(host, port)
                  .Build())
              .Build();

            client.StartAsync(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        public void Publish(string topic, byte[] payload)
        {
            try
            {
                var message = new MqttApplicationMessageBuilder()
                   .WithTopic(topic)
                   .WithPayload(payload)
                   .Build();

                client.PublishAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Publisher.Publish >>>: {ex.ToString()}");
            }
        }
    }
}
