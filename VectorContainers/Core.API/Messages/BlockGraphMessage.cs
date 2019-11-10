using Core.API.Model;

namespace Core.API.Messages
{
    public class BlockGraphMessage
    {
        public BlockGraphProto BlockGraph { get; }

        public BlockGraphMessage(BlockGraphProto blockGraph )
        {
            BlockGraph = blockGraph;
        }
    }
}
