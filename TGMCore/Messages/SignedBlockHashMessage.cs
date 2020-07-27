// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
{
    public class SignedBlockHashMessage
    {
        public string Stamp { get; }
        public ulong Node { get; }
        public ulong Round { get; }
        public byte[] PublicKey { get; }

        public SignedBlockHashMessage(string stamp, ulong node, ulong round, byte[] publicKey)
        {
            Stamp = stamp;
            Node = node;
            Round = round;
            PublicKey = publicKey;
        }
    }
}
