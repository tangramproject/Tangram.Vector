using Newtonsoft.Json;
using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwimProtocol
{
    public class CompositeMessage 
    {
        [JsonProperty(PropertyName = "ms")]
        public IReadOnlyList<SignedSwimMessage> Messages { get; private set; }

        public CompositeMessage() { }
        public CompositeMessage(IEnumerable<SignedSwimMessage> messages) => (Messages) = (messages.ToList().AsReadOnly());
        public CompositeMessage(SignedSwimMessage swimMessage) => (Messages) = (new List<SignedSwimMessage> { swimMessage });
    }
}
