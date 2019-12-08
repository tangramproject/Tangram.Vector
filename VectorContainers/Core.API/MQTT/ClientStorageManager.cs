using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;

namespace Core.API.MQTT
{
    public class ClientStorageManager : IManagedMqttClientStorage
    {
        private const string filename = @"RetainedMessages.json";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            IList<ManagedMqttApplicationMessage> retainedMessages;

            if (!File.Exists(filename))
            {
                retainedMessages = new List<ManagedMqttApplicationMessage>();
            }
            else
            {
                var json = File.ReadAllText(filename);
                retainedMessages = JsonConvert.DeserializeObject<List<ManagedMqttApplicationMessage>>(json);
            }

            return Task.FromResult(retainedMessages);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public Task SaveQueuedMessagesAsync(IList<ManagedMqttApplicationMessage> messages)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(messages));
            return Task.FromResult(0);
        }
    }
}
