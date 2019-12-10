using System;
namespace Core.API.MQTT
{
    public class NodeEndPoint
    {
        public string Host { get; }
        public int Port { get; }

        public NodeEndPoint(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}
