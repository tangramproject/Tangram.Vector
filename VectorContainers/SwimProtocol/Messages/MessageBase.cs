using Newtonsoft.Json;
using NUlid;
using SwimProtocol.Converters;
using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SwimProtocol
{
    [JsonConverter(typeof(MessageConverter))]
    public class MessageBase
    {
        [JsonProperty(PropertyName = "sn")]
        [JsonConverter(typeof(SwimNodeConverter))]
        public ISwimNode SourceNode { get; set; }

        [JsonProperty(PropertyName = "cid")]
        public Ulid? CorrelationId { get; set; }

        [JsonProperty(PropertyName = "mt")]
        public MessageType MessageType { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public virtual int GetMessageOverrideWeight(MessageBase message)
        {
            return 0;
        }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                if (CorrelationId.HasValue)
                {
                    var time = CorrelationId.Value.Time;

                    var now = DateTime.UtcNow;

                    var acceptablePast = now.AddMinutes(-2);
                    var acceptableFuture = now.AddSeconds(30);

                    if (time < acceptablePast)
                    {
                        Debug.WriteLine("Message expired.");
                        return false;
                    }

                    if (time > acceptableFuture)
                    {
                        Debug.WriteLine("Invalid message, time component too far into future.");
                        return false;
                    }
                }
                else
                {
                    Debug.WriteLine("Invalid message, no correlation id.");
                    return false;
                }

                return true;
            }
        }
    }
}

