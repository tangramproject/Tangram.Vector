using System.Threading.Tasks;
using MQTTnet;

namespace Broker.API.Node
{
    public interface INode
    {
        Task ReplicateMessage(MqttApplicationMessage message);
        Task<bool> Start();
    }
}
