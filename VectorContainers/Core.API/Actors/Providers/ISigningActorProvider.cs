using System.Threading.Tasks;
using Core.API.Messages;
using Core.API.Models;

namespace Core.API.Actors.Providers
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
