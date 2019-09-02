using Core.API.Model;
using Newtonsoft.Json;
using NUlid;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class MessageBase
    {
        [JsonProperty(PropertyName = "sn")]
        public SwimNode SourceNode { get; set; }

        [JsonProperty(PropertyName = "cid")]
        public Ulid? CorrelationId { get; set; }

        [JsonProperty(PropertyName = "mt")]
        public MessageType MessageType { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                if (CorrelationId.HasValue)
                {
                    var time = CorrelationId.Value.Time;

                    var acceptablePast = time.AddMinutes(-2);
                    var acceptableFuture = time.AddSeconds(30);

                    if (time < acceptablePast || time > acceptableFuture)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public byte[] PublicKey => throw new NotImplementedException();
    }
}
