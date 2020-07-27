using System.Collections.Generic;
using Akka.Cluster;
using Akka.Configuration;
using static Akka.Cluster.ClusterEvent;

namespace TGMCore.Actors.ClusterStrategy
{
    public class StaticQuorum : IDowning
    {
        public int QuorumSize { get; }
        public string Role { get; }

        public StaticQuorum(int quorumSize, string role = null)
        {
            QuorumSize = quorumSize;
            Role = role;
        }

        public StaticQuorum(Config config)
            : this(
                  quorumSize: config.GetInt("akka.cluster.split-brain-resolver.static-quorum.quorum-size"),
                  role: config.GetString("akka.cluster.split-brain-resolver.static-quorum.role"))
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clusterState"></param>
        /// <returns></returns>
        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            var members = clusterState.GetMembers(Role);
            var unreachable = clusterState.GetUnreachableMembers(Role);
            int availableCount = members.Count - unreachable.Count;

            return availableCount < QuorumSize
                // too few available, down our partition
                ? clusterState.GetMembers()
                // enough available, down unreachable
                : clusterState.GetUnreachableMembers();
        }
    }
}
