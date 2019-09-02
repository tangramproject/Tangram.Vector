using System.Numerics;

namespace Core.API.ZKP
{
    public interface IProver
    {
        BigInteger X { get; }

        BigInteger R(BigInteger challenge);
        BigInteger T();
        BigInteger Y();
    }
}