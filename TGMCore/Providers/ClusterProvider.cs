// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using TGMCore.Actors.ClusterStrategy;

namespace TGMCore.Providers
{
    public class ClusterProvider : IClusterProvider
    {
        private readonly Cluster _cluster;
        private readonly int _quorumSize;

        public ClusterProvider(ActorSystem actorSystem, Config config)
        {
            _cluster = Cluster.Get(actorSystem);
            _quorumSize = config.GetInt("akka.cluster.split-brain-resolver.static-quorum.quorum-size");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public IEnumerable<Member> GetMembers(string role = "")
        {
            var members = _cluster.State.GetMembers(role);
            var unreachable = _cluster.State.GetUnreachableMembers(role);
            int availableCount = members.Count - unreachable.Count;

            return availableCount < _quorumSize
                // too few available, down our partition
                ? _cluster.State.GetMembers()
                // enough available, down unreachable
                : _cluster.State.GetUnreachableMembers();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetInitialQuorumSize()
        {
            return _quorumSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong GetSelfUniqueAddress()
        {
            return (ulong)_cluster.SelfUniqueAddress.Uid;
        }
    }
}
