using Akka.Actor;

namespace TGMCore.Services
{
    public interface IActorSystemService
    {
        ActorSystem Get { get; }

        void Start(string name = null, string configFile = null);
        void Stop();
    }
}