using Newtonsoft.Json;
using System;
using System.Web;
using System.Linq;
using LiteDB;

namespace SwimProtocol
{
    public interface ISwimNode : IEquatable<ISwimNode>
    {
        string Endpoint { get; set; }
        string Hostname { get; }
        SwimNodeStatus Status { get; set; }
        void SetStatus(SwimNodeStatus status);
        DateTime? LastModified { get; set; }
        DateTime? DeadTimestamp { get; set; }
        DateTime? SuspiciousTimestamp { get; set; }
    }
}
