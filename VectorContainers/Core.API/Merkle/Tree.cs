using System;
using System.Collections.Generic;
using System.Linq;
using Core.API.Extentions;
using Core.API.Helper;

namespace Core.API.Merkle
{
    public class Tree
    {
        public Node RootNode { get; protected set; }

        protected List<Node> nodes;
        protected List<Node> leaves;

        public static void Contract(Func<bool> action, string msg)
        {
            if (!action())
                throw new Exception(msg);
        }

        public Tree()
        {
            nodes = new List<Node>();
            leaves = new List<Node>();
        }

        public Node AppendLeaf(Node node)
        {
            nodes.Add(node);
            leaves.Add(node);

            return node;
        }

        public void AppendLeaves(Node[] nodes)
        {
            nodes.ForEach(n => AppendLeaf(n));
        }

        public Node AppendLeaf(Hash hash)
        {
            var node = CreateNode(hash);
            nodes.Add(node);
            leaves.Add(node);

            return node;
        }

        public List<Node> AppendLeaves(Hash[] hashes)
        {
            hashes.ForEach(h => new List<Node>().Add(AppendLeaf(h)));
            return new List<Node>();
        }

        public Hash AddTree(Tree tree)
        {
            Contract(() => leaves.Count > 0, "Cannot add to a tree with no leaves.");
            tree.leaves.ForEach(l => AppendLeaf(l));

            return BuildTree();
        }

        /// <summary>
        /// If we have an odd number of leaves, add a leaf that
        /// is a duplicate of the last leaf hash so that when we add the leaves of the new tree,
        /// we don't change the root hash of the current tree.
        /// This method should only be used if you have a specific reason that you need to balance
        /// the last node with it's right branch, for example as a pre-step to computing an audit trail
        /// on the last leaf of an odd number of leaves in the tree.
        /// </summary>
        public void FixOddNumberLeaves()
        {
            if ((leaves.Count & 1) == 1)
            {
                var lastLeaf = leaves.Last();
                var l = AppendLeaf(lastLeaf.Hash);
                // l.Text = lastLeaf.Text;
            }
        }

        /// <summary>
        /// Builds the tree for leaves and returns the root node.
        /// </summary>
        public Hash BuildTree()
        {
            // We do not call FixOddNumberLeaves because we want the ability to append 
            // leaves and add additional trees without creating unecessary wasted space in the tree.
            Contract(() => leaves.Count > 0, "Cannot build a tree with no leaves.");
            BuildTree(leaves);

            return RootNode.Hash;
        }

        // Why would we need this?
        //public void RegisterRoot(Node node)
        //{
        //    Contract(() => node.Parent == null, "Node is not a root node.");
        //    rootNode = node;
        //}

        /// <summary>
        /// Returns the audit proof hashes to reconstruct the root hash.
        /// </summary>
        /// <param name="leafHash">The leaf hash we want to verify exists in the tree.</param>
        /// <returns>The audit trail of hashes needed to create the root, or an empty list if the leaf hash doesn't exist.</returns>
        public List<ProofHash> AuditProof(Hash leafHash)
        {
            List<ProofHash> auditTrail = new List<ProofHash>();

            var leafNode = FindLeaf(leafHash);

            if (leafNode != null)
            {
                Contract(() => leafNode.Parent != null, "Expected leaf to have a parent.");
                var parent = leafNode.Parent;
                BuildAuditTrail(auditTrail, parent, leafNode);
            }

            return auditTrail;
        }

        /// <summary>
        /// Verifies ordering and consistency of the first n leaves, such that we reach the expected subroot.
        /// This verifies that the prior data has not been changed and that leaf order has been preserved.
        /// m is the number of leaves for which to do a consistency check.
        /// </summary>
        public List<ProofHash> ConsistencyProof(int m)
        {
            // Rule 1:
            // Find the leftmost node of the tree from which we can start our consistency proof.
            // Set k, the number of leaves for this node.
            List<ProofHash> hashNodes = new List<ProofHash>();
            int idx = (int)Math.Log(m, 2);

            // Get the leftmost node.
            Node node = leaves[0];

            // Traverse up the tree until we get to the node specified by idx.
            while (idx > 0)
            {
                node = node.Parent;
                --idx;
            }

            int k = node.Leaves().Count();
            hashNodes.Add(new ProofHash(node.Hash, ProofHash.Branch.OldRoot));

            if (m == k)
            {
                // Continue with Rule 3 -- the remainder is the audit proof
            }
            else
            {
                // Rule 2:
                // Set the initial sibling node (SN) to the sibling of the node acquired by Rule 1.
                // if m-k == # of SN's leaves, concatenate the hash of the sibling SN and exit Rule 2, as this represents the hash of the old root.
                // if m - k < # of SN's leaves, set SN to SN's left child node and repeat Rule 2.

                // sibling node:
                Node sn = node.Parent.RightNode;
                bool traverseTree = true;

                while (traverseTree)
                {
                    Contract(() => sn != null, "Sibling node must exist because m != k");
                    int sncount = sn.Leaves().Count();

                    if (m - k == sncount)
                    {
                        hashNodes.Add(new ProofHash(sn.Hash, ProofHash.Branch.OldRoot));
                        break;
                    }

                    if (m - k > sncount)
                    {
                        hashNodes.Add(new ProofHash(sn.Hash, ProofHash.Branch.OldRoot));
                        sn = sn.Parent.RightNode;
                        k += sncount;
                    }
                    else // (m - k < sncount)
                    {
                        sn = sn.LeftNode;
                    }
                }
            }

            // Rule 3: Apply ConsistencyAuditProof below.

            return hashNodes;
        }

        /// <summary>
        /// Completes the consistency proof with an audit proof using the last node in the consistency proof.
        /// </summary>
        public List<ProofHash> ConsistencyAuditProof(Hash nodeHash)
        {
            List<ProofHash> auditTrail = new List<ProofHash>();

            var node = RootNode.Single(n => n.Hash == nodeHash);
            var parent = node.Parent;
            BuildAuditTrail(auditTrail, parent, node);

            return auditTrail;
        }

        /// <summary>
        /// Verify that if we walk up the tree from a particular leaf, we encounter the expected root hash.
        /// </summary>
        public static bool VerifyAudit(Hash rootHash, Hash leafHash, List<ProofHash> auditTrail)
        {
            Contract(() => auditTrail.Count > 0, "Audit trail cannot be empty.");
            Hash testHash = leafHash;

            // TODO: Inefficient - compute hashes directly.
            foreach (ProofHash auditHash in auditTrail)
            {
                testHash = auditHash.Direction == ProofHash.Branch.Left ?
                    Hash.Create(testHash.Value.Concat(auditHash.Hash.Value).ToArray()) :
                    Hash.Create(auditHash.Hash.Value.Concat(testHash.Value).ToArray());
            }

            return rootHash == testHash;
        }

        /// <summary>
        /// For demo / debugging purposes, we return the pairs of hashes used to verify the audit proof.
        /// </summary>
        public static List<Tuple<Hash, Hash>> AuditHashPairs(Hash leafHash, List<ProofHash> auditTrail)
        {
            Contract(() => auditTrail.Count > 0, "Audit trail cannot be empty.");
            var auditPairs = new List<Tuple<Hash, Hash>>();
            Hash testHash = leafHash;

            // TODO: Inefficient - compute hashes directly.
            foreach (ProofHash auditHash in auditTrail)
            {
                switch (auditHash.Direction)
                {
                    case ProofHash.Branch.Left:
                        auditPairs.Add(new Tuple<Hash, Hash>(testHash, auditHash.Hash));
                        testHash = Hash.Create(testHash.Value.Concat(auditHash.Hash.Value).ToArray());
                        break;

                    case ProofHash.Branch.Right:
                        auditPairs.Add(new Tuple<Hash, Hash>(auditHash.Hash, testHash));
                        testHash = Hash.Create(auditHash.Hash.Value.Concat(testHash.Value).ToArray());
                        break;
                }
            }

            return auditPairs;
        }

        public static bool VerifyConsistency(Hash oldRootHash, List<ProofHash> proof)
        {
            Hash hash, lhash, rhash;

            if (proof.Count > 1)
            {
                lhash = proof[proof.Count - 2].Hash;
                int hidx = proof.Count - 1;
                hash = rhash = Tree.ComputeHash(lhash, proof[hidx].Hash);
                hidx -= 2;

                // foreach (var nextHashNode in proof.Skip(1))
                while (hidx >= 0)
                {
                    lhash = proof[hidx].Hash;
                    hash = rhash = Tree.ComputeHash(lhash, rhash);

                    --hidx;
                }
            }
            else
            {
                hash = proof[0].Hash;
            }

            return hash == oldRootHash;
        }

        public static Hash ComputeHash(Hash left, Hash right)
        {
            return Hash.Create(left.Value.Concat(right.Value).ToArray());
        }

        protected void BuildAuditTrail(List<ProofHash> auditTrail, Node parent, Node child)
        {
            if (parent != null)
            {
                Contract(() => child.Parent == parent, "Parent of child is not expected parent.");
                var nextChild = parent.LeftNode == child ? parent.RightNode : parent.LeftNode;
                var direction = parent.LeftNode == child ? ProofHash.Branch.Left : ProofHash.Branch.Right;

                // For the last leaf, the right node may not exist.  In that case, we ignore it because it's
                // the hash we are given to verify.
                if (nextChild != null)
                {
                    auditTrail.Add(new ProofHash(nextChild.Hash, direction));
                }

                BuildAuditTrail(auditTrail, child.Parent.Parent, child.Parent);
            }
        }

        protected Node FindLeaf(Hash leafHash)
        {
            // TODO: We can improve the search for the leaf hash by maintaining a sorted list of leaf hashes.
            // We use First because a tree with an odd number of leaves will duplicate the last leaf
            // and will therefore have the same hash.
            return leaves.FirstOrDefault(l => l.Hash == leafHash);
        }

        /// <summary>
        /// Reduce the current list of n nodes to n/2 parents.
        /// </summary>
        /// <param name="nodes"></param>
        protected void BuildTree(List<Node> nodes)
        {
            Contract(() => nodes.Count > 0, "node list not expected to be empty.");

            if (nodes.Count == 1)
            {
                RootNode = nodes[0];
            }
            else
            {
                List<Node> parents = new List<Node>();

                for (int i = 0; i < nodes.Count; i += 2)
                {
                    Node right = (i + 1 < nodes.Count) ? nodes[i + 1] : null;
                    // Constructing the Node resolves the right node being null.
                    Node parent = CreateNode(nodes[i], right);
                    parents.Add(parent);
                }

                BuildTree(parents);
            }
        }

        // Override in derived class to extend the behavior.
        // Alternatively, we could implement a factory pattern.

        protected virtual Node CreateNode(Hash hash)
        {
            return new Node(hash);
        }

        protected virtual Node CreateNode(Node left, Node right)
        {
            return new Node(left, right);
        }

    }
}
