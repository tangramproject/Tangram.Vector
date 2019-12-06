using Core.API.Model;

namespace Core.API.Messages
{
    public class SignedBlockGraphMessage<TAttach>
    {
        public ulong Node { get; }
        public BaseGraphProto<TAttach> BlockGraph { get; }
        public ulong Round { get; }
        public byte[] PublicKey { get; }

        public SignedBlockGraphMessage(ulong nodeId, BaseGraphProto<TAttach> blockGraph, ulong round, byte[] publicKey)
        {
            Node = nodeId;
            BlockGraph = blockGraph;
            Round = round;
            PublicKey = publicKey;
        }
    }
}
