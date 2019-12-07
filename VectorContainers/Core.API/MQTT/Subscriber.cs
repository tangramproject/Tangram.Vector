using System;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;

namespace Core.API.MQTT
{
    public class Subscriber
    {
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> MqttApplicationMessageReceived;

        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly string topic;
        private readonly ILogger logger;
        private readonly IManagedMqttClient client;

        public Subscriber(ulong id, string host, int port, string topic)
        {
            this.id = id;
            this.host = host;
            this.port = port;
            this.topic = topic;

            var loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<Subscriber>();

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
                    .WithClientId($"Subscriber-{id}")
                    .WithTcpServer(host, port)
                    .Build())
                .Build();

            client.UseApplicationMessageReceivedHandler(OnMqttApplicationMessageReceived);
            client.SubscribeAsync(topic);
            client.StartAsync(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnMqttApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            if (MqttApplicationMessageReceived == null)
            {
                logger.LogWarning("<<< Subscriber.OnMqttApplicationMessageReceived >>>: Mqtt messages will not be received. Event handler delegate not defined.");
            }

            MqttApplicationMessageReceived?.Invoke(this, e);
        }
    }
}
