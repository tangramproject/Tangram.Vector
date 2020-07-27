// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMCore.Model;

namespace TGMCore.Messages
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
