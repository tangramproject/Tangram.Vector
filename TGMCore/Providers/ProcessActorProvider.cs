// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using Akka.Actor;
using TGMCore.Messages;
using Microsoft.Extensions.Logging;
using TGMCore.Actors;
using TGMCore.Services;

namespace TGMCore.Providers
{
    public class ProcessActorProvider<TAttach> : IProcessActorProvider<TAttach>
    {
        private readonly IActorRef actor;

        public ProcessActorProvider(IActorSystemService actorSystemService, ISigningActorProvider signingActorProvider, ILogger<ProcessActorProvider<TAttach>> logger)
        {
            var actorProps = ProcessActor<TAttach>.Create(signingActorProvider);
            actor = actorSystemService.Get.ActorOf(actorProps, "process-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> Process(BlockGraphMessage<TAttach> message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
