using System;
using Core.API.Model;

namespace Core.API.Messages
{
    public class VerifiyBlockSignatureMessage<TAttach>
    {
        public BaseBlockIDProto<TAttach> BlockID { get; }

        public VerifiyBlockSignatureMessage(BaseBlockIDProto<TAttach> blockID)
        {
            BlockID = blockID;
        }
    }
}
