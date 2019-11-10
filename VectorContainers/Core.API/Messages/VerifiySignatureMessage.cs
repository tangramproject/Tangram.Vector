namespace Core.API.Messages
{
    public class VerifiySignatureMessage
    {
        public byte[] Signature { get; }
        public byte[] Message { get; }
        public byte[] PublicKey { get; }

        public VerifiySignatureMessage(byte[] signature, byte[] message, byte[] publicKey)
        {
            Signature = signature;
            Message = message;
            PublicKey = publicKey;
        }
    }
}
