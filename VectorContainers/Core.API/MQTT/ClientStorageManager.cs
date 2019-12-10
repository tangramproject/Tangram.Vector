using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace Core.API.MQTT
{
    public class ClientStorageManager : IManagedMqttClientStorage
    {
        private readonly string filename = @"RetainedMessages.json";
        private IList<ManagedMqttApplicationMessage> retainedMessages = new List<ManagedMqttApplicationMessage>();

        public ClientStorageManager()
        {

        }

        public ClientStorageManager(string filename)
        {
            this.filename = filename;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                File.Delete(filename);
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IList<ManagedMqttApplicationMessage> GetRetainedMessages()
        {
            return new ReadOnlyCollection<ManagedMqttApplicationMessage>(retainedMessages);
        }
    }
}
