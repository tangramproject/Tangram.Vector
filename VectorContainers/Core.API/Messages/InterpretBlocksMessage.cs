using System.Collections.Generic;
using System.Linq;
using Core.API.Consensus;

namespace Core.API.Messages
{
    public class InterpretBlocksMessage
    {
        public ulong Node { get; }
        public IEnumerable<BlockID> BlockIDs { get; } = Enumerable.Empty<BlockID>();

        public InterpretBlocksMessage(ulong node, IEnumerable<BlockID> blockIDs)
        {
            Node = node;
            BlockIDs = blockIDs;
        }
    }
}
