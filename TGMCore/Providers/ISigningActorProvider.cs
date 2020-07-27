// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMCore.Messages;
using TGMCore.Models;

namespace TGMCore.Providers
{
    public interface ISigningActorProvider
    {
        Task<KeyPairMessage> CreateKeyPurpose(KeyPurposeMessage message);
        Task<SignedHashResponse> Sign(SignedBlockMessage message);
        Task<SignedHashResponse> Sign(SignedHashMessage message);
        Task<bool> VerifiyBlockSignature<TModel>(VerifiyBlockSignatureMessage<TModel> message);
        Task<bool> VerifiySignature(VerifySignatureMessage message);
    }
}
