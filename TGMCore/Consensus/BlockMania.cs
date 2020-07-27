// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Text;

namespace TGMCore.Consensus
{
    public class BlockID : IEquatable<BlockID>
    {
        private static readonly string hexUpper = "0123456789ABCDEF";

        public string Hash { get; }
        public ulong Node { get; }
        public ulong Round { get; }
        public object SignedBlock { get; }

        public BlockID()
        {
            Hash = string.Empty;
            Node = 0;
            Round = 0;
        }

        public BlockID(string hash)
        {
            Hash = hash;
            Node = 0;
            Round = 0;
        }

        public BlockID(string hash, ulong node, ulong round)
        {
            Hash = hash;
            Node = node;
            Round = round;
        }

        public BlockID(string hash, ulong node, ulong round, object singedBlock)
        {
            Hash = hash;
            Node = node;
            Round = round;
            SignedBlock = singedBlock;
        }

        public bool Valid() => Hash != string.Empty;

        public override string ToString()
        {
            var v = new StringBuilder();
            v.Append(Node.ToString());
            v.Append(" | ");
            v.Append(Round.ToString());

            if (!string.IsNullOrEmpty(Hash))
            {
                v.Append(" | ");
                for (int i = 6; i < 12; i++)
                {
                    var c = Hash[i];
                    v.Append(new char[] { hexUpper[c >> 4], hexUpper[c & 0x0f] });
                }
            }

            return v.ToString();
        }

        public bool Equals(BlockID blockID)
        {
            return blockID != null
                && blockID.Node == Node
                && blockID.Round == Round
                && Hash.Equals(blockID.Hash);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Node, Round, Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BlockID);
        }
    }

    public class BlockGraph
    {
        public BlockID Block { get; }
        public List<Dep> Deps { get; }
        public BlockID Prev { get; }

        public BlockGraph()
        {
            Block = new BlockID();
            Deps = new List<Dep>();
            Prev = new BlockID();
        }

        public BlockGraph(BlockID block)
        {
            Block = block;
            Deps = new List<Dep>();
            Prev = new BlockID();
        }

        public BlockGraph(BlockID block, BlockID prev)
        {
            Block = block;
            Prev = prev;
            Deps = new List<Dep>();
        }

        public BlockGraph(BlockID block, List<Dep> deps, BlockID prev)
        {
            Block = block;
            Deps = deps;
            Prev = prev;
        }
    }

    public class Dep
    {
        public BlockID Block { get; }
        public List<BlockID> Deps { get; }
        public BlockID Prev { get; }

        public Dep()
        {
            Block = new BlockID();
            Deps = new List<BlockID>();
            Prev = new BlockID();
        }

        public Dep(BlockID block)
        {
            Block = block;
            Deps = new List<BlockID>();
            Prev = new BlockID();
        }

        public Dep(BlockID block, List<BlockID> deps)
        {
            Block = block;
            Deps = deps;
            Prev = new BlockID();
        }

        public Dep(BlockID block, List<BlockID> deps, BlockID prev)
        {
            Block = block;
            Deps = deps;
            Prev = prev;
        }
    }

    public class Interpreted
    {
        public List<BlockID> Blocks { get; }
        public ulong Consumed { get; }
        public ulong Round { get; }

        public Interpreted(List<BlockID> blocks, ulong consumed, ulong round)
        {
            Blocks = blocks;
            Consumed = consumed;
            Round = round;
        }
    }
}
