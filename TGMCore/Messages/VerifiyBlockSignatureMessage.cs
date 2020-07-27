// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMCore.Model;

namespace TGMCore.Messages
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
