using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster;
using static Akka.Cluster.ClusterEvent;

namespace TGMCore.Actors.ClusterStrategy
{
    internal static class CurrentClusterStateExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private static bool ShouldConsider(Member member, string role = null) =>
            string.IsNullOrWhiteSpace(role)
                ? member.Status == MemberStatus.Up
                : member.Status == MemberStatus.Up && member.HasRole(role);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        private static bool ShouldConsiderForUnreachable(Member member, string role = null) =>
            string.IsNullOrWhiteSpace(role) | member.HasRole(role);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool HasAvailableMember(this CurrentClusterState state, Address address)
        {
            var member = state.Members.FirstOrDefault(m => m.Address.Equals(address));
            var unavilable = state.Unreachable.FirstOrDefault(m => m.Address.Equals(address));

            return member != null && unavilable == null && member.Status == MemberStatus.Up;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static ImmutableHashSet<Member> GetAvailableMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => CurrentClusterStateExtensions.ShouldConsider(member, role);

            return state.Members.Where(ShouldConsider)
                .Except(state.Unreachable.Where(ShouldConsider))
                .ToImmutableHashSet();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static ImmutableHashSet<Member> GetUnreachableMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => ShouldConsiderForUnreachable(member, role);

            return state.Unreachable.Where(ShouldConsider)
                .ToImmutableHashSet();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static ImmutableHashSet<Member> GetMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => CurrentClusterStateExtensions.ShouldConsider(member, role);

            return state.Members.Where(ShouldConsider)
                .ToImmutableHashSet();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public static ImmutableSortedSet<Member> SortByAge(this IEnumerable<Member> members)
        {
            return members.ToImmutableSortedSet(new AgeComparer());
        }

        /// <summary>
        /// 
        /// </summary>
        private class AgeComparer : IComparer<Member>
        {
            public int Compare(Member a, Member b)
            {
                if (a.Equals(b)) return 0;
                if (a.IsOlderThan(b)) return -1;
                return 1;
            }
        }
    }
}
