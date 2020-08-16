using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Remote;
using TGMCore.Services;

namespace TGMCore.Actors
{
    public class TerminatorActor: ReceiveActor
    {
        private readonly IActorService _actorService;

        public TerminatorActor(IActorService actorService)
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
