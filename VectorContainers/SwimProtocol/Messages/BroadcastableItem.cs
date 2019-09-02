using Newtonsoft.Json;
using SwimProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SwimProtocol
{
    public class BroadcastableItem : IEquatable<BroadcastableItem>
    {
        [JsonProperty(PropertyName = "m")]
        public SignedSwimMessage SwimMessage { get; private set; }

        [JsonIgnore]
        public int BroadcastCount { get; set; }

        public BroadcastableItem(SignedSwimMessage swimMessage) => (SwimMessage) = (swimMessage);

        public bool Equals(BroadcastableItem other)
        {
            if (other == null)
                return false;

            if (SwimMessage.Hash.SequenceEqual(other.SwimMessage.Hash) && SwimMessage.Signature.SequenceEqual(other.SwimMessage.Signature))
                return true;

            return false;
        }
    }
}
