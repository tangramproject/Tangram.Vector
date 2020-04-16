namespace Core.API.Messages
{
    public class HeaderMessage
    {
        public int Difficulty { get; }
        public string Proof { get; }
        public string PublicKey { get; }
        public string Rnd { get; }
        public string Seed { get; }
        public string Security { get; }
        public string Signature { get; }
        public string BulletProof { get; }
        public object TransactionModel { get; }
        public string Nonce { get; }
        public int MinStake { get; }

        public HeaderMessage(int difficulty, string proof, string publicKey, string nonce, string rnd, string seed, string signature, string bulletProof, object transactionModel, int minStake)
        {
            Difficulty = difficulty;
            Proof = proof;
            PublicKey = publicKey;
            Nonce = nonce;
            Rnd = rnd;
            Seed = seed;
            Signature = signature;
            BulletProof = bulletProof;
            TransactionModel = transactionModel;
            MinStake = minStake;
        }
    }
}
