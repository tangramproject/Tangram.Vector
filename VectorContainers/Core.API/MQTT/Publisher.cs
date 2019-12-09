using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Extensions.ManagedClient;
using Serilog;
using Serilog.Events;

namespace Core.API.MQTT
{
    public class Publisher
    {
        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly IManagedMqttClient client;
        private readonly ClientStorageManager clientStorageManager;

        public Publisher(ulong id, string host, int port)
        {
            this.id = id;
            this.host = host;
            this.port = port;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("MQTT.Publisher.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2)
                .CreateLogger();

            client = new MqttFactory().CreateManagedMqttClient();

            client.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(e =>
            {
                Log.Error($"<<< Publisher >>>: Connecting failed! {e.Exception.ToString()}");
            });

            clientStorageManager = new ClientStorageManager();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsConnected => client.IsConnected;

        /// <summary>
        /// 
        /// </summary>
        public async Task<bool> Start()
        {
            var options = new ManagedMqttClientOptionsBuilder()
              .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
              .WithStorage(clientStorageManager)
              .WithClientOptions(new MqttClientOptionsBuilder()
                  .WithClientId($"Publisher-{id}")
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
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        public async Task<MqttClientPublishResult> Publish(string topic, byte[] payload)
        {
            MqttClientPublishResult result = default;

            try
            {
                var message = new MqttApplicationMessageBuilder()
                   .WithTopic(topic)
                   .WithPayload(payload)
                   .WithAtLeastOnceQoS()
                   .Build();

                result = await client.PublishAsync(message);
            }
            catch (Exception ex)
            {
                Log.Error($"<<< Publisher.Publish >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IList<ManagedMqttApplicationMessage> GetStoredMessages()
        {
            return clientStorageManager.GetMessages();
        }
    }
}
