namespace Core.API.Messages
{
    public class ProposeMessage
    {
        public string BulletProof { get; }
        public string Commit { get; }
        public object Model { get; }
        public string Seed { get; }
        public string Security { get; }
        public int MinStake { get; }
        public int MaxStake { get; } = 1000;

        public ProposeMessage(string bulletProof, string commit, object model, string seed, string security, int minStake)
        {
            BulletProof = bulletProof;
            Commit = commit;
            Model = model;
            Seed = seed;
            Security = security;
            MinStake = minStake;
        }
    }
}
