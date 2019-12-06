using Core.API.Model;

namespace Core.API.Messages
{
    public class LastInterpretedMessage<TAttach>
    {
        public ulong Last { get; }
        public BaseBlockIDProto<TAttach> BlockID { get; }

        public LastInterpretedMessage(ulong last, BaseBlockIDProto<TAttach> blockID)
        {
            Last = last;
            BlockID = blockID;
        }
    }
}
