// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using TGMCore.Consensus;

namespace TGMCore.Model
{
    public interface IBaseGraphProto<TAttach>
    {
        string Id { get; set; }
        bool Included { get; set; }
        bool Replied { get; set; }
        BaseBlockIDProto<TAttach> Block { get; set; }
        List<DepProto<TAttach>> Deps { get; set; }
        BaseBlockIDProto<TAttach> Prev { get; set; }

        bool Equals(object obj);
        bool Equals(BaseGraphProto<TAttach> other);
        int GetHashCode();
        BlockGraph ToBlockGraph();
    }
}