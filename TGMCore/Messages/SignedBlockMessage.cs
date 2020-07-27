// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
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
