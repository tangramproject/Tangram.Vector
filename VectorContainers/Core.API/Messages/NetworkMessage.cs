using System;
using System.Collections.Generic;
using Core.API.Model;

namespace Core.API.Messages
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