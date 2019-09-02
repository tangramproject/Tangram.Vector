using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Swim
{
    public interface ISwimClient
    {
        IEnumerable<SwimNode> Members { get; }
        IEnumerable<SwimNode> GetRandomMembers(int size);
        Task ProtocolLoop();
    }
}