// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
{
    public class VerifySignatureMessage
    {
        public byte[] Signature { get; }
        public byte[] Message { get; }
        public byte[] PublicKey { get; }

        public VerifySignatureMessage(byte[] signature, byte[] message, byte[] publicKey)
        {
            Signature = signature;
            Message = message;
            PublicKey = publicKey;
        }
    }
}
