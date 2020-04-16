using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Core.API.Messages;
using Microsoft.AspNetCore.DataProtection;

namespace Core.API.Actors.Providers
{
    public class VerifiableFunctionsActorProvider : IVerifiableFunctionsActorProvider
    {
        private readonly IActorRef actor;

        public VerifiableFunctionsActorProvider(ActorSystem actorSystem, Props props)
        {
            actor = actorSystem.ActorOf(props, "vf-actor");
        }

        public VerifiableFunctionsActorProvider(ActorSystem actorSystem, IDataProtectionProvider dataProtectionProvider)
        {
            var actorProps = VerifiableFunctionsActor.Create(dataProtectionProvider);
            actor = actorSystem.ActorOf(actorProps, "vf-actor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<KeyPairMessage> GetKeyPair()
        {
            return await actor.Ask<KeyPairMessage>(new KeyPairMessage(null, null));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<HeaderMessage> ProposeNewBlock(ProposeMessage message)
        {
            return await actor.Ask<HeaderMessage>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<int> Difficulty(VDFDifficultyMessage message)
        {
            return await actor.Ask<int>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifyDifficulty(VerifyDifficultyMessage message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VerifyVDF(VeifyVDFMessage message)
        {
            return await actor.Ask<bool>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<byte[]> Sign(SignedHashMessage message)
        {
            return await actor.Ask<byte[]>(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> VeriySignature(VerifySignatureMessage message)
        {
            return await actor.Ask<bool>(message);
        }
    }
}
