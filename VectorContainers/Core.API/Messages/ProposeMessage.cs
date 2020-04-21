namespace Core.API.Messages
{
    public class ProposeMessage
    {
        public byte[] BulletProof { get; }
        public byte[] Commit { get; }
        public long Height { get; }
        public byte[] Model { get; }
        public byte[] Nonce { get; }
        public byte[] Seed { get; }
        public byte[] Security { get; }
        public int MinStake { get; }
        public int MaxStake { get; } = 1000;

        public ProposeMessage(byte[] bulletProof, byte[] commit,long height, byte[] model, byte[] nonce, byte[] seed, byte[] security, int minStake)
        {
            BulletProof = bulletProof;
            Commit = commit;
            Height = height;
            Model = model;
            Nonce = nonce;
            Seed = seed;
            Security = security;
            MinStake = minStake;
        }
    }
}
