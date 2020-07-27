// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Numerics;
using TGMCore.Helper;

namespace TGMCore.VDF
{
    public struct CipherPair
    {
        public BigInteger C;
        public bool Positive;
    }

    public class Sloth
    {
        private static readonly BigInteger Zero = new BigInteger();
        private static readonly BigInteger One = new BigInteger(1);
        private static readonly BigInteger Two = new BigInteger(2);
        private static readonly BigInteger Three = new BigInteger(3);
        private static readonly BigInteger Four = new BigInteger(4);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="m"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public CipherPair EncodeByte(int t, byte[] m, BigInteger p)
        {
            var encryptedM = new BigInteger(m);
            for (int x = 0; x < t; x++)
            {
                encryptedM = Square(encryptedM, p);
            }
            if (IsQuadraticResidue(new BigInteger(m), p))
            {
                return new CipherPair { C = encryptedM, Positive = true };
            }

            return new CipherPair { C = encryptedM, Positive = false };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="m"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public CipherPair Encode32(int t, uint m, BigInteger p)
        {
            var encryptedM = new BigInteger((long)m);
            for (int x = 0; x < t; x++)
            {
                encryptedM = Square(encryptedM, p);
            }
            if (IsQuadraticResidue(new BigInteger((long)m), p))
            {
                return new CipherPair { C = encryptedM, Positive = true };
            }

            return new CipherPair { C = encryptedM, Positive = false };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="cipherPair"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public BigInteger Decode(int t, CipherPair cipherPair, BigInteger p)
        {
            var c = cipherPair.C;
            var z = ModSqrtOp(t, c, p);
            if (cipherPair.Positive)
            {
                return z;
            }

            return Util.Mod(BigInteger.Negate(z), p);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public string Eval(int t, BigInteger x, BigInteger p)
        {
            var y = ModSqrtOp(t, x, p);
            return y.ToString();
        }   

        /// <summary>
        ///     
        /// </summary>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool Verify(int t, BigInteger x, BigInteger y, BigInteger p)
        {
            if (!IsQuadraticResidue(x, p))
            {
                x = Util.Mod(BigInteger.Negate(x), p);
            }
            for (int i = 0; i < t; i++)
            {
                y = Square(y, p);
            }

            return x.CompareTo(y) == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="exponent"></param>
        /// <param name="modulus"></param>
        /// <returns></returns>
        private BigInteger ModExp(BigInteger value, BigInteger exponent, BigInteger modulus)
        {
            return BigInteger.ModPow(value, exponent, modulus);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool IsQuadraticResidue(BigInteger x, BigInteger p)
        {
            var t = ModExp(x, Div(Sub(p, One), Two), p);
            return t.CompareTo(One) == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private BigInteger Mul(BigInteger x, BigInteger y)
        {
            return BigInteger.Multiply(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private BigInteger Add(BigInteger x, BigInteger y)
        {
            return BigInteger.Add(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private BigInteger Sub(BigInteger x, BigInteger y)
        {
            return BigInteger.Subtract(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private BigInteger Div(BigInteger x, BigInteger y)
        {
            return BigInteger.Divide(x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private BigInteger ModSqrt(BigInteger x, BigInteger p)
        {
            BigInteger y;
            if (IsQuadraticResidue(x, p))
            {
                y = ModExp(x, Div(Add(p, One), Four), p);
            }
            else
            {
                x = Util.Mod(BigInteger.Negate(x), p);
                y = ModExp(x, Div(Add(p, One), Four), p);
            }

            return y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private BigInteger Square(BigInteger y, BigInteger p)
        {
            return ModExp(y, Two, p);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private BigInteger ModSqrtOp(int t, BigInteger x, BigInteger p)
        {
            BigInteger y = x;
            for (int i = 0; i < t; i++)
            {
                y = ModSqrt(y, p);
            }

            return y;
        }
    }
}
