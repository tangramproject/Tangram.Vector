// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMCore.Model;

namespace TGMCore.Messages
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
