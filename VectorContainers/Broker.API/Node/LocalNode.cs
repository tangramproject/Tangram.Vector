using System.Threading.Tasks;
using MQTTnet;

namespace Broker.API.Node
{
    public class LocalNode: INode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task ReplicateMessage(MqttApplicationMessage message)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<bool> Start()
        {
            throw new System.NotImplementedException();
        }
    }
}
