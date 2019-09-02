using System.Numerics;

namespace Core.API.ZKP
{
    public interface IVerifier
    {
        bool Verify(Proof proof);
    }
}