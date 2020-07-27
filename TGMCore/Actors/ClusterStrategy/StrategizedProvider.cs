using System;
using Akka.Actor;
using Akka.Cluster;

namespace TGMCore.Actors.ClusterStrategy
{
    public abstract class StrategizedProvider : IDowningProvider
    {
        protected ActorSystem System { get; private set; }
        private readonly string _rootConfigElement;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="rootConfigElement"></param>
        public StrategizedProvider(ActorSystem system, string rootConfigElement)
        {
            System = system;
            _rootConfigElement = rootConfigElement;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual TimeSpan DownRemovalMargin =>
            System.Settings.Config
            .GetTimeSpan($"akka.cluster.{_rootConfigElement}.down-removal-margin", @default: TimeSpan.FromSeconds(10));

        /// <summary>
        /// 
        /// </summary>
        public Props DowningActorProps =>
            Props.Create(() =>
            new ClusterListenerActor(StableAfter, GetDowningStrategy()));

        protected abstract IDowning GetDowningStrategy();

        /// <summary>
        /// 
        /// </summary>
        protected virtual TimeSpan StableAfter =>
            System.Settings.Config
            .GetTimeSpan($"akka.cluster.{_rootConfigElement}.stable-after", @default: TimeSpan.FromSeconds(10));
    }
}
