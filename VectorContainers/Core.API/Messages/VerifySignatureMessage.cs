namespace Core.API.Messages
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
