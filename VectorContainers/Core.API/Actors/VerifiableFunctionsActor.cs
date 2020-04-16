using System;
using System.IO;
using System.Numerics;
using Akka.Actor;
using Akka.Event;
using Core.API.Extentions;
using Core.API.LibSodium;
using Core.API.Messages;
using libsignal.ecc;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Core.API.Actors
{
    public class VerifiableFunctionsActor : ReceiveActor
    {
        public const int DefualtMiningDifficulty = 20555;

        public const string Seed = "6b341e59ba355e73b1a8488e75b617fe1caa120aa3b56584a217862840c4f7b5d70cefc0d2b36038d67a35b3cd406d54f8065c1371a17a44c1abb38eea8883b2";
        public const string Security256 = "60464814417085833675395020742168312237934553084050601624605007846337253615407";

        private static readonly DirectoryInfo coreDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        private const string keyFilePurpose = "VerifiableFunctionsActor.Key";

        private readonly ILoggingAdapter logger;
        private readonly IDataProtector dataProtector;
        private readonly string protectedPayload;
        
        public VerifiableFunctionsActor(IDataProtectionProvider dataProtectionProvider)
        {
            logger = Context.GetLogger();
            dataProtector = dataProtectionProvider.CreateProtector(keyFilePurpose);

            Receive<KeyPairMessage>(message => Sender.Tell(GetKeyPair()));
            Receive<ProposeMessage>(message => Sender.Tell(ProposeNewBlock(message)));
            Receive<VDFDifficultyMessage>(message => Sender.Tell(Difficulty(message)));
            Receive<VerifySignatureMessage>(message => Sender.Tell(VeriySignature(message)));
            Receive<VeifyVDFMessage>(messag => Sender.Tell(VerifyVDF(messag)));
            Receive<VerifyDifficultyMessage>(messag => Sender.Tell(VerifyDifficulty(messag)));
            Receive<SignedHashMessage>(message => Sender.Tell(Sign(message)));

            //var keyPath = Path.Combine(coreDirectory.ToString(), $"{keyFilePurpose}");

            //if (!File.Exists(keyPath))
            //{
            //    protectedPayload = dataProtector.Protect(Newtonsoft.Json.JsonConvert.SerializeObject(CreateKeyPair()));
            //    SaveFile(keyPath, protectedPayload);

            //    return;
            //}

            //protectedPayload = File.ReadAllText(keyPath);


            protectedPayload = dataProtector.Protect(JsonConvert.SerializeObject(CreateKeyPair()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyPairMessage GetKeyPair()
        {
            if (string.IsNullOrEmpty(protectedPayload))
                throw new ArgumentException("Protected payload is not set.", nameof(protectedPayload));

            var unprotectedPayload = dataProtector.Unprotect(protectedPayload);
            var definition = new { SecretKey = "", PublicKey = "" };
            var message = JsonConvert.DeserializeAnonymousType(unprotectedPayload, definition);

            return new KeyPairMessage(message.SecretKey, message.PublicKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public HeaderMessage ProposeNewBlock(ProposeMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var keyPair = GetKeyPair();
            var input = Cryptography.GenericHashNoKey($"{message.Commit} {message.BulletProof} {message.Seed} {message.Security} {message.MinStake}");
            var proof = Curve.calculateVrfSignature(Curve.decodePrivatePoint(keyPair.SecretKey.FromHex()), input);
            var vrfBytes = Curve.verifyVrfSignature(Curve.decodePoint(keyPair.PublicKey.FromHex(), 0), input, proof);
            var difficulty = Difficulty(new VDFDifficultyMessage(vrfBytes, message.MinStake, message.MaxStake));
            var sloth = new Vdf.Sloth();
            var nonce = sloth.Eval(difficulty, new BigInteger(vrfBytes), BigInteger.Parse(message.Security));
            var signature = Sign(new SignedHashMessage(Helper.Util.SerializeProto(message.Model))).ToHex();
            var headerMessage = new HeaderMessage(difficulty, proof.ToHex(), keyPair.PublicKey, nonce, vrfBytes.ToHex(), Seed, signature, message.BulletProof, message.Model, message.MinStake);

            return headerMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="security"></param>
        /// <returns></returns>
        public bool VerifyVDF(VeifyVDFMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var sloth = new Vdf.Sloth();
            return sloth.Verify(message.Header.Difficulty, BigInteger.Parse(message.Header.Proof), BigInteger.Parse(message.Header.Nonce), BigInteger.Parse(message.Security));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] Sign(SignedHashMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var keyPair = GetKeyPair();
            var signedHash = Curve.calculateSignature(Curve.decodePrivatePoint(keyPair.SecretKey.FromHex()), message.Hash);

            return signedHash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public int Difficulty(VDFDifficultyMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var stake = (float)message.MinStake / message.MaxStake;
            var entropy = Helper.Util.ShannonEntropy(message.VrfBytes.ToHex());
            var slot = 1 / stake;
            var pslot = Math.Pow(entropy, slot - 1);

            return (int)Math.Abs(Math.Round(pslot * 2000, 1));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool VeriySignature(VerifySignatureMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return Curve.verifySignature(Curve.decodePoint(message.PublicKey, 0), message.Message, message.Signature);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool VerifyDifficulty(VerifyDifficultyMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var difficulty = Difficulty(new VDFDifficultyMessage(message.VrfBytes, message.MinStake, message.MaxStake));
            return difficulty == message.Difficulty;
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
        /// <param name="path"></param>
        /// <param name="content"></param>
        private void SaveFile(string path, string content)
        {
            try
            {
                using StreamWriter outputFile = new StreamWriter(path);
                outputFile.WriteLine(content);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Props Create(IDataProtectionProvider dataProtectionProvider) =>
            Props.Create(() => new VerifiableFunctionsActor(dataProtectionProvider));
    }
}