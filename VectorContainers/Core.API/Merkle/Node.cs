using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.API.Extentions;
using Core.API.Helper;

namespace Core.API.Merkle
{
    public class Node : IEnumerable<Node>
    {
        public Hash Hash { get; protected set; }
        public Node LeftNode { get; protected set; }
        public Node RightNode { get; protected set; }
        public Node Parent { get; protected set; }

        public bool IsLeaf { get { return LeftNode == null && RightNode == null; } }

        public Node()
        {
        }

        /// <summary>
        /// Constructor for a base node (leaf), representing the lowest level of the tree.
        /// </summary>
        public Node(Hash hash)
        {
            Hash = hash;
        }

        /// <summary>                
        /// Constructor for a parent node.
        /// </summary>
        public Node(Node left, Node right = null)
        {
            LeftNode = left;
            RightNode = right;
            LeftNode.Parent = this;
            RightNode.IfNotNull(r => r.Parent = this);
            ComputeHash();
        }

        public override string ToString()
        {
            return Hash.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Node> GetEnumerator()
        {
            foreach (var n in Iterate(this)) yield return n;
        }

        /// <summary>
        /// Bottom-up/left-right iteration of the tree.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<Node> Iterate(Node node)
        {
            if (node.LeftNode != null)
            {
                foreach (var n in Iterate(node.LeftNode)) yield return n;
            }

            if (node.RightNode != null)
            {
                foreach (var n in Iterate(node.RightNode)) yield return n;
            }

            yield return node;
        }

        public Hash ComputeHash(byte[] buffer)
        {
            Hash = Hash.Create(buffer);

            return Hash;
        }

        /// <summary>
        /// Return the leaves (not all children, just leaves) under this node
        /// </summary>
        public IEnumerable<Node> Leaves()
        {
            return this.Where(n => n.LeftNode == null && n.RightNode == null);
        }

        public void SetLeftNode(Node node)
        {
            Tree.Contract(() => node.Hash != null, "Node hash must be initialized.");
            LeftNode = node;
            LeftNode.Parent = this;
            ComputeHash();
        }

        public void SetRightNode(Node node)
        {
            Tree.Contract(() => node.Hash != null, "Node hash must be initialized.");
            RightNode = node;
            RightNode.Parent = this;

            // Can't compute hash if the left node isn't set yet.
            if (LeftNode != null)
            {
                ComputeHash();
            }
        }

        /// <summary>
        /// True if we have enough data to verify our hash, particularly if we have child nodes.
        /// </summary>
        /// <returns>True if this node is a leaf or a branch with at least a left node.</returns>
        public bool CanVerifyHash()
        {
            return (LeftNode != null && RightNode != null) || (LeftNode != null);
        }

        /// <summary>
        /// Verifies the hash for this node against the computed hash for our child nodes.
        /// If we don't have any children, the return is always true because we have nothing to verify against.
        /// </summary>
        public bool VerifyHash()
        {
            if (LeftNode == null && RightNode == null)
            {
                return true;
            }

            if (RightNode == null)
            {
                return Hash.Equals(LeftNode.Hash);
            }

            Tree.Contract(() => LeftNode != null, "Left branch must be a node if right branch is a node.");
            Hash leftRightHash = Hash.Create(LeftNode.Hash, RightNode.Hash);

            return Hash.Equals(leftRightHash);
        }

        /// <summary>
        /// If the hashes are equal, we know the entire node tree is equal.
        /// </summary>
        public bool Equals(Node node)
        {
            return Hash.Equals(node.Hash);
        }

        protected void ComputeHash()
        {
            // Repeat the left node if the right node doesn't exist.
            // This process breaks the case of doing a consistency check on 3 leaves when there are only 3 leaves in the tree.
            //Hash rightHash = RightNode == null ? LeftNode.Hash : RightNode.Hash;
            //Hash = Hash.Create(LeftNode.Hash.Value.Concat(rightHash.Value).ToArray());

            // Alternativately, do not repeat the left node, but carry the left node's hash up.
            // This process does not break the edge case described above.
            // We're implementing this version because the consistency check unit tests pass when we don't simulate
            // a right-hand node.
            Hash = RightNode == null ?
                LeftNode.Hash : //Hash.Create(LeftNode.Hash.Value.Concat(LeftNode.Hash.Value).ToArray()) : 
                Hash.Create(LeftNode.Hash.Value.Concat(RightNode.Hash.Value).ToArray());
            Parent?.ComputeHash();      // Recurse, because out hash has changed.
        }

    }
}
