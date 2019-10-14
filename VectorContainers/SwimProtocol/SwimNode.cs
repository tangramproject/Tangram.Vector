using Newtonsoft.Json;
using System;
using System.Web;
using System.Linq;
using LiteDB;

namespace SwimProtocol
{
    public class SwimNode : ISwimNode
    {
        private string _endpoint;
        private SwimNodeStatus _status;
        private DateTime? _deadTimestamp;
        private DateTime? _suspiciousTimestamp;
        private DateTime? _lastModified;

        public SwimNode()
        {
            Status = SwimNodeStatus.Unknown;
        }

        [JsonProperty(PropertyName = "ep")]
        public string Endpoint { get => _endpoint; set => _endpoint = NormalizeEndpoint(value); }

        [JsonIgnore]
        [BsonId]
        public string Hostname { get => new Uri(_endpoint).Host.Replace(".onion", string.Empty); }

        [JsonIgnore]
        public SwimNodeStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public void SetStatus(SwimNodeStatus status)
        {
            if (status == SwimNodeStatus.Dead && Status != SwimNodeStatus.Dead)
            {
                DeadTimestamp = DateTime.UtcNow;
            }

            if (status == SwimNodeStatus.Suspicious && Status != SwimNodeStatus.Suspicious && Status != SwimNodeStatus.Dead)
            {
                SuspiciousTimestamp = DateTime.UtcNow;
            }

            LastModified = DateTime.UtcNow;

            _status = status;
        }

        [JsonIgnore]
        public DateTime? LastModified
        {
            get { return _lastModified; }
            set { _lastModified = value; }
        }

        [JsonIgnore]
        public DateTime? DeadTimestamp
        {
            get { return _deadTimestamp; }
            set { _deadTimestamp = value; }
        }

        [JsonIgnore]
        public DateTime? SuspiciousTimestamp
        {
            get { return _suspiciousTimestamp; }
            set { _suspiciousTimestamp = value; }
        }

        public SwimNode(string endpoint) => Endpoint = endpoint;

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
            if(other != null)
                return Endpoint == other.Endpoint;

            return false;
        }
        public bool Equals(ISwimNode other)
        {
            return Equals((SwimNode)other);
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