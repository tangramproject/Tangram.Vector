using Newtonsoft.Json;
using System;

namespace Swim
{
    public class SwimNode : IEquatable<SwimNode>
    {
        private string _endpoint;

        [JsonProperty(PropertyName = "ep")]
        public string Endpoint { get => _endpoint; set => _endpoint = NormalizeEndpoint(value); }

        public SwimNode(string endpoint) => (Endpoint) = (endpoint);

        private static string NormalizeEndpoint(string endpoint)
        {
            return endpoint.ToLowerInvariant().TrimEnd(new char[] { '/', '\\' });
        }

        public override string ToString()
        {
            return $"Node: {Endpoint}";
        }

        public bool Equals(SwimNode other)
        {
            return Endpoint == other.Endpoint;
        }

        public override bool Equals(object obj)
        {
            var n = obj as SwimNode;

            if (n != null)
            {
                return Equals(n);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _endpoint.GetHashCode();
        }

        public static bool operator ==(SwimNode obj1, SwimNode obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null))
            {
                return false;
            }

            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1._endpoint == obj2._endpoint;
        }

        public static bool operator !=(SwimNode obj1, SwimNode obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
