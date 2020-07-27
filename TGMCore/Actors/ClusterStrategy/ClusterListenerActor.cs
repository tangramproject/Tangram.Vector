using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using static Akka.Cluster.ClusterEvent;

namespace TGMCore.Actors.ClusterStrategy
{
    public class ClusterListenerActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly Cluster _cluster;
        private readonly TimeSpan _stableAfter;
        private readonly IDowning _downingStrategy;

        private readonly ILoggingAdapter _log = Context.GetLogger();

        public IStash Stash { get; set; }

        public ClusterListenerActor(TimeSpan stableAfter, IDowning downingStrategy)
        {
            _stableAfter = stableAfter;
            _downingStrategy = downingStrategy;

            _cluster = Cluster.Get(Context.System);
            _cluster.Subscribe(Self, SubscriptionInitialStateMode.InitialStateAsSnapshot, new[] { typeof(IClusterDomainEvent) });

            Receive<CurrentClusterState>(msg => Become(() => WaitingForStability(msg)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clusterState"></param>
        private void Stable(CurrentClusterState clusterState)
        {
            Receive<IClusterDomainEvent>(msg =>
            {
                Stash.Stash();
                Become(() => WaitingForStability(clusterState));
                Stash.UnstashAll();
            });

            if (clusterState.Leader != null && clusterState.Leader.Equals(_cluster.SelfAddress))
            {
                _log.Info($"Checking downing strategy {_downingStrategy.GetType().Name} for leader {clusterState.Leader} on node {_cluster.SelfAddress}");

                foreach (var victim in _downingStrategy.GetVictims(clusterState))
                {
                    _log.Warning($"Leader ({clusterState.Leader}) Downing victim {victim}");
                    _cluster.Down(victim.Address);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clusterState"></param>
        private void WaitingForStability(CurrentClusterState clusterState)
        {
            _log.Debug($"Waiting {_stableAfter.TotalSeconds} seconds for cluster stability");

            var timeoutHandle = Context.System.Scheduler.ScheduleTellOnceCancelable(
                _stableAfter,
                Self,
                new Timeout(),
                Self);

            Receive<Timeout>(msg =>
            {
                Become(() => Stable(clusterState));
            });

            Receive<IClusterDomainEvent>(msg =>
            {
                timeoutHandle.CancelIfNotNull();

                CurrentClusterState newState;

                switch (msg)
                {
                    case LeaderChanged changed:
                        newState = clusterState.Copy(leader: changed.Leader);
                        break;
                    case RoleLeaderChanged changed:
                        var roleLeaders = clusterState.AllRoles
                        .Select(role => (Role: role, Leader: clusterState.RoleLeader(role)))
                        .Where(t => t.Leader != null)
                        .ToImmutableDictionary(t => t.Role, t => t.Leader);

                        newState = clusterState.Copy(roleLeaderMap: roleLeaders.SetItem(changed.Role, changed.Leader));
                        break;
                    case MemberRemoved member:
                        newState = clusterState.Copy(
                            members: clusterState.Members.Remove(member.Member),
                            unreachable: clusterState.Unreachable.Remove(member.Member));
                        break;
                    case MemberUp member:
                        newState = clusterState.Copy(members: clusterState.Members.Add(member.Member));
                        break;
                    case UnreachableMember member:
                        newState = clusterState.Copy(unreachable: clusterState.Unreachable.Add(member.Member));
                        break;
                    case ReachableMember member:
                        newState = clusterState.Copy(unreachable: clusterState.Unreachable.Remove(member.Member));
                        break;
                    default:
                        newState = clusterState;
                        break;
                }

                Become(() => WaitingForStability(newState));
            });
        }

        private class Timeout { }
    }
}
