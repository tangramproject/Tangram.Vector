namespace Core.API.Messages
{
    public class SignedBlockMessage
    {
        public ulong Node { get; }
        public object Model { get; }
        public ulong Round { get; }
        public byte[] PublicKey { get; }

        public SignedBlockMessage(ulong nodeId, object model, ulong round, byte[] publicKey)
        {
            Node = nodeId;
            Model = model;
            Round = round;
            PublicKey = publicKey;
        }
    }
}
