using System;
using System.Numerics;
using Core.API.Helper;
using Core.API.LibSodium;

namespace Core.API.ZKP
{
    public struct Proof
    {
        public BigInteger C { get; set; }
        public BigInteger R { get; set; }
        public BigInteger T { get; set; }
        public BigInteger Y { get; set; }
    }

    public static class NIZKProver
    {
        public const string n = "304725736006641064630309168029524485973";

        private static readonly BigInteger prime;
        private static readonly BigInteger g = 2;

        private static byte[] hash;
        private static BigInteger v;

        static NIZKProver() => prime = BigInteger.Parse(n);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static Proof GetProof(byte[] hash)
        {
            NIZKProver.hash = hash;
            v = BigInteger.Abs(new BigInteger(Cryptography.RandomBytes(16)));

            var c = BigInteger.Abs(Heuristic());
            var proof = new Proof { C = c, R = R(c), T = T(), Y = T() };

            //Console.WriteLine($"C: {proof.C.ToString()}\nR: {proof.R.ToString()}\nT: {proof.T.ToString()}\nY: {proof.Y.ToString()}");
            //Console.WriteLine($"v: {v.ToString()}");
            //Console.WriteLine($"X: {X().ToString()}");

            return proof;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static BigInteger GetHashStringNumber(byte[] hash)
        {
            NIZKProver.hash = hash;
            return X();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static BigInteger R(BigInteger c)
        {
            return BigInteger.Subtract(v, BigInteger.Multiply(c, X()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static BigInteger T()
        {
            return BigInteger.ModPow(g, v, prime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static BigInteger Y()
        {
            return BigInteger.ModPow(g, X(), prime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static BigInteger X()
        {
            return Util.ConvertHashToNumber(hash, prime, 8);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static BigInteger Heuristic()
        {
            var h = Cryptography.GenericHashNoKey(g.ToString() + Y().ToString() + T().ToString());
            return BitConverter.ToInt32(h, 0) % prime;
        }
    }
}
