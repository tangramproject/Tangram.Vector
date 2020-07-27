// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Extentions;
using TGMCore.Helper;
using TGMCore.Messages;
using TGMCore.Model;
using libsignal.ecc;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace TGMCore.Actors
{
    public class SigningActor : ReceiveActor
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILoggingAdapter _logger;
        private readonly IUnitOfWork _unitOfWork;

        private IDataProtector _dataProtector;
        private DataProtectionPayloadProto _protectionPayloadProto;

        public SigningActor(IDataProtectionProvider dataProtectionProvider, IUnitOfWork unitOfWork)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _unitOfWork = unitOfWork;
            
            _logger = Context.GetLogger();

            ReceiveAsync<KeyPurposeMessage>(async message => Sender.Tell(await CreateKeyPurpose(message)));
            ReceiveAsync<SignedHashMessage>(async message => Sender.Tell(await Sign(message)));
            Receive<SignedBlockMessage>(message => Sender.Tell(Sign(message)));
            Receive<VerifySignatureMessage>(message => Sender.Tell(VerifiySignature(message)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private KeyPairMessage GetKeyPair()
        {
            if (_protectionPayloadProto == null)
                throw new NullReferenceException("ProtectionPayloadProto cannot be null");

            if (string.IsNullOrEmpty(_protectionPayloadProto.Payload))
                throw new ArgumentException("Protected payload is not set.", nameof(_protectionPayloadProto.Payload));

            var unprotectedPayload = _dataProtector.Unprotect(_protectionPayloadProto.Payload);
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
                _dataProtector = _dataProtectionProvider.CreateProtector(message.Purpose);
                _protectionPayloadProto = await _unitOfWork.DataProtectionPayload.GetFirstOrDefault(x => x.FriendlyName == message.Purpose);

                if (_protectionPayloadProto == null)
                {
                    _protectionPayloadProto = new DataProtectionPayloadProto
                    {
                        FriendlyName = message.Purpose,
                        Payload = _dataProtector.Protect(JsonConvert.SerializeObject(CreateKeyPair()))
                    };

                    var stored = await _unitOfWork.DataProtectionPayload.StoreOrUpdate(_protectionPayloadProto);
                    if (stored == null)
                    {
                        _logger.Error($"<<< SigningActor.CreateKeyPurpose >>>: Unable to save protection key payload for: {message.Purpose}");
                        return null;
                    }
                }

                return GetKeyPair();
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< SigningActor.CreateKeyPurpose >>>: {ex}");
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
                _logger.Error($"<<< SigningActor.Sign >>>: {ex}");
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

            if (string.IsNullOrEmpty(message.KeyPurpose))
                throw new ArgumentNullException(nameof(message.Hash));

            try
            {
                var keyPair = await CreateKeyPurpose(new KeyPurposeMessage(message.KeyPurpose));
                var signedHashResponse = new Models.SignedHashResponse
                {
                    PublicKey = keyPair.PublicKey.FromHex(),
                    Signature = Curve.calculateSignature(Curve.decodePrivatePoint(keyPair.SecretKey.FromHex()), message.Hash)
                };

                return signedHashResponse;
            }
            catch (Exception ex)
            {
                _logger.Error($"<<< SigningActor.Sign >>>: {ex}");
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
                _logger.Error($"<<< SigningActor.VerifiySignature >>>: {ex}");
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
