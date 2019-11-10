using System;
using Core.API.Model;

namespace Core.API.Messages
{
    public class VerifiyBlockSignatureMessage
    {
        public BlockIDProto BlockID { get; }

        public VerifiyBlockSignatureMessage(BlockIDProto blockID)
        {
            BlockID = blockID;
        }
    }
}
