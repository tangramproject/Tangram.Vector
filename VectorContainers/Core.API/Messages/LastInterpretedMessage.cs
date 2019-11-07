using System;
using Core.API.Model;

namespace Core.API.Messages
{
    public class LastInterpretedMessage
    {
        public ulong Last { get; }
        public BlockIDProto BlockID { get; }

        public LastInterpretedMessage(ulong last, BlockIDProto blockID)
        {
            Last = last;
            BlockID = blockID;
        }
    }
}
