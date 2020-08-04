// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using Akka.Cluster;

namespace TGMCore.Providers
{
    public interface IClusterProvider
    {
        IEnumerable<Member> GetMembers(string role = "");
        ClusterEvent.CurrentClusterState GetCurrentClusterState();
        ulong GetSelfUniqueAddress();
        int GetInitialQuorumSize();
        int AvailableMembersCount(string role = "");
    }
}