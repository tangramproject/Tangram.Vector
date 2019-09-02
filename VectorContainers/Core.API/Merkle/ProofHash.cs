namespace Core.API.Merkle
{
    public class ProofHash
    {
        public enum Branch
        {
            Left,
            Right,
            OldRoot
        }

        public Hash Hash { get; protected set; }
        public Branch Direction { get; protected set; }

        public ProofHash(Hash hash, Branch direction)
        {
            Hash = hash;
            Direction = direction;
        }

        public override string ToString()
        {
            return Hash.ToString();
        }
    }
}
