// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMCore.Messages;

namespace TGMCore.Providers
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