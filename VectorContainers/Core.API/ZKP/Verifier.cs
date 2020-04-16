using System;
using System.Numerics;
using Core.API.Helper;

namespace Core.API.ZKP
{
    public class Verifier : IVerifier
    {
        public const string n = "304725736006641064630309168029524485973";
        private readonly BigInteger prime;
        private readonly BigInteger g = 2;

        public Verifier()
        {
            prime = BigInteger.Parse(n);
            g = PickGenerator(prime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proof"></param>
        /// <returns></returns>
        public bool Verify(Proof proof)
        {
            BigInteger result;

            if (proof.R < 0)
            {
                var inverValue = BigInteger.ModPow(g, -proof.R, prime);
                var f = BigInteger.ModPow(proof.Y, proof.C, prime);
                var inv = Inverse(inverValue, prime);

                result = BigInteger.Multiply(inv, f) % prime;
            }
            else
            {
                var q = BigInteger.ModPow(g, proof.R, prime);
                var k = BigInteger.ModPow(proof.Y, proof.C, prime) % prime;

                result = BigInteger.Multiply(q, k);
            }

            return proof.T.Equals(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static BigInteger GetChallenge()
        {
            return new BigInteger(Util.SecureRandom(32).ToByteArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static BigInteger PickGenerator(BigInteger p)
        {
            for (BigInteger i = 1; i < p; i++)
            {
                var rand = i;
                var exp = 1;
                var next = Util.Mod(rand, p);
                while (next != 1)
                {
                    next = Util.Mod(next * rand, p);
                    exp++;
                    if (exp == p - 1)
                        return rand;
                }
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="quotient"></param>
        private void BitWise(ref BigInteger v1, ref BigInteger v2, BigInteger quotient)
        {
            var x = BigInteger.Subtract(v2, BigInteger.Multiply(quotient, v1));
            var y = v1;

            if (x != y)
            {
                x ^= y;
                y ^= x;
                x ^= y;

                v1 = y;
                v2 = x;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private Tuple<BigInteger, BigInteger, BigInteger> ExtendedEuclidean(BigInteger a, BigInteger b)
        {
            var s1 = new BigInteger(0);
            var s2 = new BigInteger(1);
            var t1 = new BigInteger(1);
            var t2 = new BigInteger(0);
            var r1 = b;
            var r2 = a;

            while (!r1.Equals(0))
            {
                var quotient = BigInteger.Divide(r2, r1);

                BitWise(ref r1, ref r2, quotient);
                BitWise(ref s1, ref s2, quotient);
                BitWise(ref t1, ref t2, quotient);
            }

            return new Tuple<BigInteger, BigInteger, BigInteger>(r2, s2, t2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inverN"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private BigInteger Inverse(BigInteger inverN, BigInteger p)
        {
            var gcd = ExtendedEuclidean(inverN, p);
            if (!gcd.Item1.Equals(1))
            {
                throw new Exception("has no multiplicative inverse modulo");
            }

            return Util.Mod(Convert.ToInt32(gcd.Item2.ToString()), Convert.ToInt32(p.ToString()));
        }
    }
}
