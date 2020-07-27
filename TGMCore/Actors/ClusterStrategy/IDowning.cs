using System.Collections.Generic;
using Akka.Cluster;
using static Akka.Cluster.ClusterEvent;

namespace TGMCore.Actors.ClusterStrategy
{
    public interface IDowning
    {
        IEnumerable<Member> GetVictims(CurrentClusterState clusterState);
    }
}
