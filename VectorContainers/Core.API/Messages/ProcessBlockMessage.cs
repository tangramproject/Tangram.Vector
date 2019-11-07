using Core.API.Model;

namespace Core.API.Messages
{
    public class ProcessBlockMessage
    {
        public BlockGraphProto BlockGraph { get; }

        public ProcessBlockMessage(BlockGraphProto blockGraph)
        {
            BlockGraph = blockGraph;
        }
    }
}
