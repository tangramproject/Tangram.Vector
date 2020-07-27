// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Numerics;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using TGMCore.Extentions;
using TGMCore.LibSodium;
using TGMCore.Messages;
using TGMCore.Model;
using libsignal.ecc;
using TGMCore.VDF;
using TGMCore.Providers;

namespace TGMCore.Actors
{
    public class VerifiableFunctionsActor : ReceiveActor
    {
        public const int DefualtMiningDifficulty = 20555;

        public const string Seed = "6b341e59ba355e73b1a8488e75b617fe1caa120aa3b56584a217862840c4f7b5d70cefc0d2b36038d67a35b3cd406d54f8065c1371a17a44c1abb38eea8883b2";
        public const string Security256 = "60464814417085833675395020742168312237934553084050601624605007846337253615407";

        private const string _keyPurpose = "VerifiableFunctionsActor.Key";

        private readonly ILoggingAdapter _logger;
        private readonly ISigningActorProvider _signingActorProvider;

        public VerifiableFunctionsActor(ISigningActorProvider signingActorProvider)
        {
            _signingActorProvider = signingActorProvider;

            _logger = Context.GetLogger();
  
            ReceiveAsync<ProposeMessage>(async message => Sender.Tell(await ProposeNewBlock(message)));
            Receive<VDFDifficultyMessage>(message => Sender.Tell(Difficulty(message)));
            Receive<VeifyVDFMessage>(messag => Sender.Tell(VerifyVDF(messag)));
            Receive<VerifyDifficultyMessage>(messag => Sender.Tell(VerifyDifficulty(messag)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<HeaderMessage> ProposeNewBlock(ProposeMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var keyPairMessage = await _signingActorProvider.CreateKeyPurpose(new KeyPurposeMessage(_keyPurpose));
            var input = Cryptography.GenericHashNoKey($"{message.Commit} {message.BulletProof} {message.Seed} {message.Security} {message.MinStake}");
            var proof = Curve.calculateVrfSignature(Curve.decodePrivatePoint(keyPairMessage.SecretKey.FromHex()), input);
            var vrfBytes = Curve.verifyVrfSignature(Curve.decodePoint(keyPairMessage.PublicKey.FromHex(), 0), input, proof);
            var difficulty = Difficulty(new VDFDifficultyMessage(vrfBytes, message.MinStake, message.MaxStake));
            var sloth = new Sloth();
            var nonce = sloth.Eval(difficulty, new BigInteger(vrfBytes), BigInteger.Parse(message.Security.ToHex()));
            var signedHashResponse = await _signingActorProvider.Sign(new SignedHashMessage(Helper.Util.SerializeProto(message.Model), _keyPurpose));
            var header = new HeaderProto()
            {
                BulletProof = message.BulletProof.ToHex(),
                Difficulty = difficulty,
                Height = message.Height,
                MinStake = message.MinStake,
                Nonce = nonce,
                PrevNonce = message.Nonce.ToHex(),
                Proof = proof.ToHex(),
                PublicKey = signedHashResponse.PublicKey.ToHex(),
                Reward = 0,
                Rnd = vrfBytes.ToHex(),
                Security = message.Security.ToHex(),
                Seed = message.Seed.ToHex(),
                Signature = signedHashResponse.Signature.ToHex(),
                TransactionModel = message.Model
            };
            var headerMessage = new HeaderMessage(header);

            return headerMessage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="security"></param>
        /// <returns></returns>
        private bool VerifyVDF(VeifyVDFMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var sloth = new Sloth();
            return sloth.Verify(message.Header.Proto.Difficulty, BigInteger.Parse(message.Header.Proto.Proof), BigInteger.Parse(message.Header.Proto.Nonce), BigInteger.Parse(message.Security));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private int Difficulty(VDFDifficultyMessage message)
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
        private bool VerifyDifficulty(VerifyDifficultyMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var difficulty = Difficulty(new VDFDifficultyMessage(message.VrfBytes, message.MinStake, message.MaxStake));
            return difficulty == message.Difficulty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="difficulty"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private ulong RewardBlock(int difficulty, long height)
        {
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Props Create(ISigningActorProvider signingActorProvider) =>
            Props.Create(() => new VerifiableFunctionsActor(signingActorProvider));
    }
}