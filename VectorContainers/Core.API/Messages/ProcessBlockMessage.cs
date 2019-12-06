using Core.API.Model;

namespace Core.API.Messages
{
    public class ProcessBlockMessage<TAttach>
    {
        public BaseGraphProto<TAttach> BlockGraph { get; }

        public ProcessBlockMessage(BaseGraphProto<TAttach> blockGraph)
        {
            BlockGraph = blockGraph;
        }
    }
}
