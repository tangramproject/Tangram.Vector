using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol.Converters
{
    public class MessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(MessageBase));
        }
        public override bool CanWrite
        {
            get { return false; }
        }

        public MessageBase Create(MessageType mt)
        {
            switch (mt)
            {
                case MessageType.Ack:
                    return new AckMessage();
                case MessageType.Alive:
                    return new AliveMessage();
                case MessageType.Dead:
                    return new DeadMessage();
                case MessageType.Ping:
                    return new PingMessage();
                case MessageType.PingReq:
                    return new PingReqMessage();
                default:
                    return null;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            object messageType;

            if (Enum.TryParse(typeof(MessageType), jo["mt"].Value<string>(), true, out messageType))
            {
                MessageBase message = Create((MessageType)messageType);

                if (message != null)
                {
                    serializer.Populate(jo.CreateReader(), message);
                }

                return message;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
