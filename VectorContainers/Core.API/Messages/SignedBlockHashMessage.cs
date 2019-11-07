namespace Core.API.Messages
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
