// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Linq;
using TGMCore.Model;

namespace TGMCore.Messages
{
    public class InterpretMessage<TAttach>
    {
        public ulong Node { get; }
        public IEnumerable<BaseBlockIDProto<TAttach>> Models { get; } = Enumerable.Empty<BaseBlockIDProto<TAttach>>();

        public InterpretMessage(ulong node, IEnumerable<BaseBlockIDProto<TAttach>> models)
        {
            Node = node;
            Models = models;
        }
    }
}
