// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Linq;
using TGMCore.Consensus;

namespace TGMCore.Messages
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
