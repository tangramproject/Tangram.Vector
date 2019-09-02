using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Swim.Messages
{
    public class BroadcastableItem
    {
        [JsonProperty(PropertyName = "m")]
        public MessageBase Message { get; private set; }

        [JsonIgnore]
        public int BroadcastCount { get; set; }

        public BroadcastableItem(MessageBase message) => (Message) = (message);
    }
}
