// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Numerics;
using System.Text;

namespace TGMCore.Extensions
{
    public struct BezoutIdentity
    {

        public BigInteger X;

        public BigInteger Y;

        public BigInteger A;

        public BigInteger B;

        public BigInteger Gcd => A * X + B * Y;

    }

    public static class BigIntegerExtentions
    {

        public static byte[] ToBigEndianBytes(this BigInteger val)
        {
            var data = val.ToByteArray();
            var len = data.Length;
            var reverse = new byte[len];
            for (var i = 0; i < len; i++)
            {
                reverse[i] = data[len - i - 1];
            }
            return reverse;
        }

        public static BigInteger Mod(this BigInteger a, BigInteger n)
        {
            var result = a % n;
            if ((result < 0 && n > 0) || (result > 0 && n < 0))
            {
                result += n;
            }
            return result;
        }

        public static BigInteger ModSqrt(this BigInteger val, BigInteger modulus)
        {
            var exp = (modulus + 1).ModDiv(4, modulus);
            return BigInteger.ModPow(val, exp, modulus);
        }

        public static BigInteger ModDiv(this BigInteger a, BigInteger b, BigInteger modulus)
        {
            return a.ModMul(b.ModInverse(modulus), modulus);
        }

        public static BigInteger ModInverse(this BigInteger val, BigInteger modulus)
        {
            return EuclidExtended(val.ModAbs(modulus), modulus).X.ModAbs(modulus);
        }

        public static bool ModEqual(BigInteger a, BigInteger b, BigInteger modulus)
        {
            return (a % modulus) == (b % modulus);
        }

        public static BigInteger ModMul(this BigInteger a, BigInteger b, BigInteger modulus)
        {
            return a * b % modulus;
        }

        public static BigInteger ModAbs(this BigInteger val, BigInteger modulus)
        {
            if (val < 0) return modulus - ((-val) % modulus);
            return val % modulus;
        }

        public static string ToHexString(this BigInteger bi)
        {
            var bytes = bi.ToByteArray();
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                var hex = b.ToString("X2");
                sb.Append(hex);
            }

            return sb.ToString();
        }

        public static BezoutIdentity EuclidExtended(BigInteger a, BigInteger b)
        {
            var s0 = new BigInteger(1);
            var t0 = new BigInteger(0);
            var s1 = new BigInteger(0);
            var t1 = new BigInteger(1);
            var r0 = a;
            var r1 = b;

            while (r1 != 0)
            {
                var quotient = BigInteger.DivRem(r0, r1, out BigInteger r2);
                var s2 = s0 - quotient * s1;
                var t2 = t0 - quotient * t1;
                s0 = s1;
                s1 = s2;
                t0 = t1;
                t1 = t2;
                r0 = r1;
                r1 = r2;
            }
            return new BezoutIdentity
            {
                A = a,
                B = b,
                X = s0,
                Y = t0
            };
        }
    }
}
