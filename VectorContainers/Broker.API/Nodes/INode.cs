using MQTTnet;

namespace Broker.API.Nodes
{
    public interface INode
    {
        void ReplicateMessage(MqttApplicationMessage applicationMessage);
        void Start();
    }
}
