using Akka.Actor;
using Akka.Configuration;
using Akka.Remote;
using TGMCore.Actors;

namespace TGMCore.Services
{
    public class ActorService : IActorService
    {
        public string Name { get; private set; }

        public string ConfigFile { get; private set; }

        public ActorSystem Get { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="configFile"></param>
        public void Start(string name = null, string configFile = null)
        {
            if (name != null)
                Name = name;

            if (configFile != null)
                ConfigFile = configFile;

            var config = Helper.ConfigurationLoader.Load(ConfigFile).WithFallback(ConfigurationFactory.Default());

            Get = ActorSystem.Create(Name, config);

            var terminatorActor = Get.ActorOf(Props.Create<TerminatorActor>());

            Get.EventStream.Subscribe(terminatorActor, typeof(ThisActorSystemQuarantinedEvent));
        }

        /// <summary>
        /// 
        /// </summary>
        public async void Stop()
        {
            await Get.Terminate();
        }
    }
}
