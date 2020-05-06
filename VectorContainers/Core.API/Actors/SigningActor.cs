using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Core.API.Extentions;
using Core.API.Helper;
using Core.API.Messages;
using Core.API.Model;
using libsignal.ecc;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Core.API.Actors
{
    public class SigningActor : ReceiveActor
    {
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly ILoggingAdapter logger;
        private readonly IUnitOfWork unitOfWork;

        private IDataProtector dataProtector;
        private DataProtectionPayloadProto protectionPayloadProto;

        public SigningActor(IDataProtectionProvider dataProtectionProvider, IUnitOfWork unitOfWork)
        {
            this.dataProtectionProvider = dataProtectionProvider;
            this.unitOfWork = unitOfWork;

            logger = Context.GetLogger();

            ReceiveAsync<KeyPurposeMessage>(async message => Sender.Tell(await CreateKeyPurpose(message)));
            Receive<SignedHashMessage>(message => Sender.Tell(Sign(message)));
            Receive<SignedBlockMessage>(message => Sender.Tell(Sign(message)));
            Receive<VerifySignatureMessage>(message => Sender.Tell(VerifiySignature(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private KeyPairMessage GetKeyPair()
        {
            if (protectionPayloadProto == null)
                throw new NullReferenceException("ProtectionPayloadProto cannot be null");

            if (string.IsNullOrEmpty(protectionPayloadProto.Payload))
                throw new ArgumentException("Protected payload is not set.", nameof(protectionPayloadProto.Payload));

            var unprotectedPayload = dataProtector.Unprotect(protectionPayloadProto.Payload);
            var definition = new { SecretKey = "", PublicKey = "" };
            var message = JsonConvert.DeserializeAnonymousType(unprotectedPayload, definition);

            return new KeyPairMessage(message.SecretKey, message.PublicKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="purpose"></param>
        /// <returns></returns>
        private async Task<KeyPairMessage> CreateKeyPurpose(KeyPurposeMessage message)
        {
            try
            {
                dataProtector = dataProtectionProvider.CreateProtector(message.Purpose);
                protectionPayloadProto = await unitOfWork.DataProtectionPayload.GetFirstOrDefault(x => x.FriendlyName == message.Purpose);

                if (protectionPayloadProto == null)
                {
                    protectionPayloadProto = new DataProtectionPayloadProto
                    {
                        FriendlyName = message.Purpose,
                        Payload = dataProtector.Protect(JsonConvert.SerializeObject(CreateKeyPair()))
                    };
                }

                return GetKeyPair();
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.CreateKeyPurpose >>>: {ex}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private KeyPairMessage CreateKeyPair()
        {
            var keys = Curve.generateKeyPair();
            var keyPairMessage = new KeyPairMessage(keys.getPrivateKey().serialize().ToHex(), keys.getPublicKey().serialize().ToHex());

            return keyPairMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Models.SignedHashResponse Sign(SignedBlockMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Model == null)
                throw new ArgumentNullException(nameof(message.Model));

            if (message.Round <= 0)
                throw new ArgumentOutOfRangeException(nameof(message.Round));

            try
            {
                var keyPair = GetKeyPair();
                var byteArray = Util.SerializeProto(message.Model);
                var signedHashResponse = new Models.SignedHashResponse
                {
                    PublicKey = keyPair.PublicKey.FromHex(),
                    Signature = Curve.calculateSignature(Curve.decodePrivatePoint(keyPair.SecretKey.FromHex()), byteArray)
                };

                return signedHashResponse;
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.Sign >>>: {ex}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Models.SignedHashResponse Sign(SignedHashMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.Hash == null)
                throw new ArgumentNullException(nameof(message.Hash));

            try
            {
                var keyPair = GetKeyPair();
                var signedHashResponse = new Models.SignedHashResponse
                {
                    PublicKey = keyPair.PublicKey.FromHex(),
                    Signature = Curve.calculateSignature(Curve.decodePrivatePoint(keyPair.SecretKey.FromHex()), message.Hash)
                };

                return signedHashResponse;
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.Sign >>>: {ex}");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool VerifiySignature(VerifySignatureMessage message)
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
                var keyPair = GetKeyPair();
                result = Curve.verifySignature(Curve.decodePoint(keyPair.PublicKey.FromHex(), 0), message.Message, message.Signature);
            }
            catch (Exception ex)
            {
                logger.Error($"<<< SigningActor.VerifiySignature >>>: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onionServiceClient"></param>
        /// <returns></returns>
        public static Props Create(IDataProtectionProvider dataProtectionProvider, IUnitOfWork unitOfWork) =>
            Props.Create(() => new SigningActor(dataProtectionProvider, unitOfWork));

    }
}
