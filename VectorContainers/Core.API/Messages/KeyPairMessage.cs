namespace Core.API.Messages
{
    public class KeyPairMessage
    {
        public string SecretKey { get; }
        public string PublicKey { get; }

        public KeyPairMessage(string secretKey, string publicKey)
        {
            SecretKey = secretKey;
            PublicKey = publicKey;
        }
    }
}
