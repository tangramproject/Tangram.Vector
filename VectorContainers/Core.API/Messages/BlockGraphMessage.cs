using Core.API.Model;

namespace Core.API.Messages
{
    public class BlockGraphMessage<TAttach>
    {
        public BaseGraphProto<TAttach> BaseGraph { get; }

        public BlockGraphMessage(BaseGraphProto<TAttach> baseGraph )
        {
            BaseGraph = baseGraph;
        }
    }
}
