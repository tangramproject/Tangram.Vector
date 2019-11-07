using Core.API.Model;

namespace Core.API.Messages
{
    public class SignedBlockGraphMessage
    {
        public ulong Node { get; }
        public BlockGraphProto BlockGraph { get; }
        public ulong Round { get; }
        public byte[] PublicKey { get; }

        public SignedBlockGraphMessage(ulong nodeId, BlockGraphProto blockGraph, ulong round, byte[] publicKey)
        {
            Node = nodeId;
            BlockGraph = blockGraph;
            Round = round;
            PublicKey = publicKey;
        }
    }
}
