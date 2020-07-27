using System;
using Akka.Actor;

namespace TGMCore.Actors.ClusterStrategy
{
    public class SplitBrainResolverProvider : StrategizedProvider
    {
        public SplitBrainResolverProvider(ActorSystem system)
            : base(system, "split-brain-resolver")
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override IDowning GetDowningStrategy()
        {
            var config = System.Settings.Config;
            var requestedStrategy = config.GetString("akka.cluster.split-brain-resolver.active-strategy");

            IDowning strategy = requestedStrategy switch
            {
                "static-quorum" => new StaticQuorum(config),
                _ => throw new NotSupportedException($"Unknown downing strategy requested"),
            };

            return strategy;
        }
    }
}
