// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
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
