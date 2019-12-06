using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Onion;
using Core.API.Signatures;

namespace Core.API.Actors
{
    public class SigningActor : ReceiveActor
    {
        private readonly IOnionServiceClient onionServiceClient;
        private readonly ILoggingAdapter logger;

        public SigningActor(IOnionServiceClient onionServiceClient)
        {
            this.onionServiceClient = onionServiceClient;

            logger = Context.GetLogger();

            Receive<VerifiySignatureMessage>(message => Sender.Tell(VerifiySignature(message)));

            ReceiveAsync<SignedHashMessage>(async message => Sender.Tell(await Sign(message)));

            ReceiveAsync<SignedBlockMessage>(async message => Sender.Tell(await Sign(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<Models.SignedHashResponse> Sign(SignedBlockMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Model == null)
                throw new ArgumentNullException(nameof(message.Model));

            if (message.Round <= 0)
                throw new ArgumentOutOfRangeException(nameof(message.Round));

            try
            {
                var byteArray = Util.SerializeProto(message.Model);
                var signedHashResponse = await onionServiceClient.SignHashAsync(byteArray);

                return signedHashResponse;
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.Sign >>>: {ex.ToString()}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<Models.SignedHashResponse> Sign(SignedHashMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            return await onionServiceClient.SignHashAsync(message.Hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool VerifiySignature(VerifiySignatureMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Signature == null)
                throw new ArgumentNullException(nameof(message.Signature));

            if (message.Message == null)
                throw new ArgumentNullException(nameof(message.Message));

            if (message.PublicKey == null)
                throw new ArgumentNullException(nameof(message.PublicKey));

            if (message.PublicKey.Length < 32)
                throw new ArgumentOutOfRangeException(nameof(message.PublicKey));

            bool result = false;

            try
            {
                result = Ed25519.Verify(message.Signature, message.Message, message.PublicKey);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.VerifiySignature >>>: {ex.ToString()}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onionServiceClient"></param>
        /// <returns></returns>
        public static Props Create(IOnionServiceClient onionServiceClient) =>
            Props.Create(() => new SigningActor(onionServiceClient));

    }
}
