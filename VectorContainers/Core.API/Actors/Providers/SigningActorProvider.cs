using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Core.API.Messages;
using Core.API.Models;
using Core.API.Onion;

namespace Core.API.Actors.Providers
{
    public class SigningActorProvider: ISigningActorProvider
    {
        private readonly IActorRef actor;

        public SigningActorProvider(ActorSystem actorSystem, Props props)
        {
            actor = actorSystem.ActorOf(props, "signing-actor");
        }

        public SigningActorProvider(ActorSystem actorSystem, IOnionServiceClient onionServiceClient)
        {
            var actorProps = SigningActor.Create(onionServiceClient).WithRouter(new RoundRobinPool(5));
            actor = actorSystem.ActorOf(actorProps, "signing-actor");
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<SignedHashResponse> Sign(SignedBlockMessage message)
        {
            return await actor.Ask<SignedHashResponse>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<SignedHashResponse> Sign(SignedHashMessage message)
        {
            return await actor.Ask<SignedHashResponse>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifiyBlockSignature<TModel>(VerifiyBlockSignatureMessage<TModel> message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifiySignature(VerifiySignatureMessage message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
