using System.Threading.Tasks;
using Core.API.Messages;

namespace Core.API.Actors.Providers
{
    public interface IVerifiableFunctionsActorProvider
    {
        Task<KeyPairMessage> GetKeyPair();
        Task<HeaderMessage> ProposeNewBlock(ProposeMessage message);
        Task<int> Difficulty(VDFDifficultyMessage message);
        Task<byte[]> Sign(SignedHashMessage message);
        Task<bool> VerifyVDF(VeifyVDFMessage message);
        Task<bool> VerifyDifficulty(VerifyDifficultyMessage message);
        Task<bool> VeriySignature(VerifySignatureMessage message);
    }
}