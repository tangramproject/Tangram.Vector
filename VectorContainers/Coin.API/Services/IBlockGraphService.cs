using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Consensus;
using Core.API.Model;

namespace Coin.API.Services
{
    public interface IBlockGraphService
    {
        Task<BlockGraphProto> AddBlockGraph(BlockGraphProto block);
        Task<BlockIDProto> GetBlockID(byte[] address);
        Task<List<BlockIDProto>> AllBlockIDs();
        Graph Graph { get; }
        Config Config { get; }
        bool ValidateRule(CoinProto coin);
        Task<BlockGraphProto> Sign(CoinProto coin, uint round);
        bool VerifiySignature(BlockIDProto blockIDProto);
        bool VerifiyHashChain(CoinProto previous, CoinProto next);
        Task<BlockIDProto> GetPrevBlockID(byte[] address);
        void Broadcast(IEnumerable<BlockGraphProto> blockGraphProtos);
        IEnumerable<string> Endpoints();
        string Hostname { get; }
        long BlockHeight();
    }
}
