using Akka.Actor;
using Akka.Remote;
using TGMCore.Services;

namespace TGMCore.Actors
{
    public class TerminatorActor: ReceiveActor
    {
        private readonly IActorSystemService _actorService;

        public TerminatorActor(IActorSystemService actorService)
        {
            _actorService = actorService;

            ReceiveAsync<ThisActorSystemQuarantinedEvent>(async m =>
            {
                var shutdownTask = CoordinatedShutdown.Get(_actorService.Get).Run(CoordinatedShutdown.ClrExitReason.Instance);
                await shutdownTask;

                _actorService.Start();
            });
        }
    }
}
