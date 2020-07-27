// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using TGMCore.Model;

namespace TGMCore.Messages
{
    public class NetworkBlockHeightMessage
    {
        public ulong Height { get; set; }
    };

    public class FullNetworkBlockHeightMessage
    {
        public IEnumerable<NodeBlockCountProto> NodeBlockCounts { get; set; }
    };

    public class BlockHeightMessage
    {
        public int Height { get; set; }
    };
}