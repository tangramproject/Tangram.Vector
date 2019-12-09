using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Serilog;
using Serilog.Events;

namespace Core.API.MQTT
{
    public class Subscriber
    {
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> MqttApplicationMessageReceived;

        private readonly ulong id;
        private readonly string host;
        private readonly int port;
        private readonly string topic;
        private readonly IManagedMqttClient client;

        public Subscriber(ulong id, string host, int port, string topic)
        {
            this.id = id;
            this.host = host;
            this.port = port;
            this.topic = topic;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File("MQTT.Subscriber.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2)
                .CreateLogger();

            client = new MqttFactory().CreateManagedMqttClient();

            client.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(e =>
            {
                Log.Error($"<<< Subscriber >>>: Connecting failed! {e.Exception.ToString()}");
            });
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
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"Subscriber-{id}")
                    .WithTcpServer(host, port)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(20))
                    .WithKeepAliveSendInterval(TimeSpan.FromSeconds(10))
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(5))
                    .WithCleanSession(false)
                    .Build())
                .Build();

            client.UseApplicationMessageReceivedHandler(OnMqttApplicationMessageReceived);


            await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
            await client.StartAsync(options);

            return client.IsStarted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnMqttApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            if (MqttApplicationMessageReceived == null)
            {
                Log.Warning("<<< Subscriber.OnMqttApplicationMessageReceived >>>: Mqtt messages will not be received. Event handler delegate not defined.");
            }

            MqttApplicationMessageReceived?.Invoke(this, args);
        }
    }
}
