
using System.Linq;
using System.Numerics;

namespace Core.API.TimeLock
{
    public struct Puzzle
    {
        public readonly BigInteger n;
        public readonly BigInteger a;
        public readonly int t;
        public readonly BigInteger cK;

        public Puzzle(BigInteger n, BigInteger a, int t, BigInteger cK)
        {
            this.n = n;
            this.a = a;
            this.t = t;
            this.cK = cK;
        }
    }

    public class LockedPuzzle
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public (byte[] key, Puzzle Puzzle) Make(int t)
        {
            var p = BigInteger.Parse(Helper.Util.GeneratePrime(512).ToString());
            var q = BigInteger.Parse(Helper.Util.GeneratePrime(512).ToString());
            var n = p * q;
            var phi = (p - 1) * (q - 1);
            var key = BigInteger.Parse(Helper.Util.SecureRandom(256).ToString());
            var a = BigInteger.Parse(Helper.Util.SecureRandom(4096).ToString());

            a %= n;

            var e = BigInteger.ModPow(2, t, phi);
            var b = BigInteger.ModPow(a, e, n);
            var ck = (key + b) % n;

            return (key.ToByteArray(), new Puzzle(n, a, t, ck));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="puzzle"></param>
        /// <returns></returns>
        public byte[] Solve(Puzzle puzzle)
        {
            var tmp = puzzle.a;
            var seq = Enumerable.Range(0, puzzle.t).ToArray();

            for (int i = 0, seqLength = seq.Length; i < seqLength; i++)
            {
                tmp = BigInteger.ModPow(tmp, 2, puzzle.n);
            }

            var key = (puzzle.cK - tmp) % puzzle.n;
            return key.ToByteArray();
        }
    }
}
