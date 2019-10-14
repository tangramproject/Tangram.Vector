using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Consensus;
using Core.API.Model;

namespace Coin.API.Services
{
    public interface IBlockGraphService
    {
        Task<BlockGraphProto> AddBlockGraph(BlockGraphProto block);
        bool ValidateRule(CoinProto coin);
        bool VerifiySignature(BlockIDProto blockIDProto);
        bool VerifiyHashChain(CoinProto previous, CoinProto next);
        Task<int> BlockHeight();
        Task<long> NetworkBlockHeight();
        Task<bool> InterpretBlocks(IEnumerable<BlockID> blocks);
    }
}
