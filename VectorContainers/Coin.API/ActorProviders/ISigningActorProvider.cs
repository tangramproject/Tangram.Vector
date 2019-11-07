using System.Threading.Tasks;
using Core.API.Messages;
using Core.API.Model;
using Core.API.Models;

namespace Coin.API.ActorProviders
{
    public interface ISigningActorProvider
    {
        Task<byte[]> BlockHash(SignedBlockHashMessage message);
        Task<byte[]> HashCoin(SignedHashCoinMessage message);
        Task<BlockGraphProto> Sign(SignedBlockGraphMessage message);
        Task<SignedHashResponse> Sign(SignedHashMessage message);
        Task<bool> ValidateCoinRule(ValidateCoinRuleMessage message);
        Task<bool> VerifiyBlockSignature(VerifiyBlockSignatureMessage message);
        Task<bool> VerifiyHashChain(VerifiyHashChainMessage message);
        Task<bool> VerifiySignature(VerifiySignatureMessage message);
    }
}
