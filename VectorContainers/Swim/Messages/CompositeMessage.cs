using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swim.Messages
{
    public class CompositeMessage : MessageBase
    {
        [JsonProperty(PropertyName = "ms")]
        public IReadOnlyList<MessageBase> Messages { get; private set; }

        public CompositeMessage(SwimNode sourceNode, IEnumerable<MessageBase> messages) => (SourceNode, MessageType, Messages) = (sourceNode, MessageType.Composite, messages.ToList().AsReadOnly());
    }
}
