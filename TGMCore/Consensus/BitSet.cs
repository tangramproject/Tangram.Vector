// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMCore.Consensus
{
    public class BitSet
    {
        public ulong[] Commits { get; set; }
        public ulong[] Prepares { get; set; }

        public BitSet() { }

        public BitSet(int size)
        {
            ulong words = 100000; //((ulong)size + 63) >> 6;
            Commits = new ulong[words];
            Prepares = new ulong[words];
        }

        public BitSet Clone()
        {
            BitSet bitset = new BitSet
            {
                Prepares = new ulong[Prepares.Length],
                Commits = new ulong[Commits.Length]
            };

            Array.Copy(Prepares, bitset.Prepares, Prepares.Length);
            Array.Copy(Commits, bitset.Commits, Commits.Length);

            return bitset;
        }

        public bool HasCommit(ulong v)
        {
            return (Commits[v >> 6] & ((ulong)1 << ((int)v & 63))) != 0;
        }

        public bool HasPrepare(ulong v)
        {
            return (Prepares[v >> 6] & ((ulong)1 << ((int)v & 63))) != 0;
        }

        public int PrepareCount()
        {
            var c = 0;
            for (int i = 0, PreparesLength = Prepares.Length; i < PreparesLength; i++)
            {
                ulong word = Prepares[i];
                c += OnesCount64(word);
            }

            return c;
        }

        public int CommitCount()
        {
            var c = 0;
            for (int i = 0, CommitsLength = Commits.Length; i < CommitsLength; i++)
            {
                ulong word = Commits[i];
                c += OnesCount64(word);
            }

            return c;
        }

        public void SetCommit(ulong v)
        {
            Commits[v >> 6] |= (ulong)1 << ((int)v & 63);
        }

        public void SetPrepare(ulong v)
        {
            Prepares[v >> 6] |= (ulong)1 << ((int)v & 63);
        }

        // cannot find similar -> copy code
        private static int OnesCount64(ulong i)
        {
            i -= (i >> 1) & 0x5555555555555555UL;
            i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }
    }

}
