using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Remote;
using TGMCore.Actors;

namespace TGMCore.Services
{
    public class ActorSystemService : IActorSystemService
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

            var terminatorActor = Get.ActorOf(Props.Create(() => new TerminatorActor(this)));
            var deadLettersSubscriberActor = Get.ActorOf<DeadLetterMonitorActor>("dl-subscriber");

            Get.EventStream.Subscribe(terminatorActor, typeof(ThisActorSystemQuarantinedEvent));
            Get.EventStream.Subscribe(deadLettersSubscriberActor, typeof(DeadLetter));
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
