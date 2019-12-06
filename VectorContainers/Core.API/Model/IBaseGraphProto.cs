using System.Collections.Generic;
using Core.API.Consensus;

namespace Core.API.Model
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