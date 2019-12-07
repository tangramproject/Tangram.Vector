using System;
using MQTTnet;

namespace Broker.API.Extentions
{
    internal static class MqttApplicationMessageExtensions
    {
        internal const string ReplicationTopic = "$internal/replica/";

        internal static bool IsReplicated(this MqttApplicationMessage message)
        {
            return message.Topic.StartsWith(ReplicationTopic, StringComparison.CurrentCulture);
        }
    }
}
