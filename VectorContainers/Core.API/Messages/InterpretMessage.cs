using System.Collections.Generic;
using System.Linq;
using Core.API.Model;

namespace Core.API.Messages
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
