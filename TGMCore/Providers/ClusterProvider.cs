// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Linq;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using TGMCore.Actors.ClusterStrategy;
using System.Threading;
using TGMCore.Services;

namespace TGMCore.Providers
{
    public class ClusterProvider : IClusterProvider
    {
        private static readonly ManualResetEvent asTerminatedEvent = new ManualResetEvent(false);

        private readonly IActorSystemService _actorSystemService;
        private readonly Cluster _cluster;
        private readonly int _quorumSize;

        public ClusterProvider(IActorSystemService actorSystemService, Config config)
        {
            _actorSystemService = actorSystemService;
            _cluster = Cluster.Get(_actorSystemService.Get);
            _cluster.RegisterOnMemberRemoved(() => MemberRemoved(_actorSystemService.Get));

            _quorumSize = config.GetInt("akka.cluster.split-brain-resolver.static-quorum.quorum-size");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ClusterEvent.CurrentClusterState GetCurrentClusterState()
        {
            return _cluster.State;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public IEnumerable<Member> GetMembers(string role = "")
        {
            var members = _cluster.State.GetMembers(role);
            return members.Where(x => x.UniqueAddress.Uid != _cluster.SelfUniqueAddress.Uid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public int AvailableMembersCount(string role = "")
        {
            var members = _cluster.State.GetMembers(role);
             return members.Where(x => x.UniqueAddress.Uid != _cluster.SelfUniqueAddress.Uid).Count();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actorSystem"></param>
        private async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();
            asTerminatedEvent.Set();
        }
    }
}
