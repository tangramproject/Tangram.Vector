namespace Core.API.Messages
{
    public class VDFDifficultyMessage
    {
        public byte[] VrfBytes { get; }
        public int MinStake { get; }
        public int MaxStake { get; }

        public VDFDifficultyMessage(byte[] vrfBytes, int minStake, int maxStake)
        {
            VrfBytes = vrfBytes;
            MinStake = minStake;
            MaxStake = maxStake;
        }
    }

    public class VerifyDifficultyMessage
    {
        public int Difficulty { get; }
        public byte[] VrfBytes { get; }
        public int MinStake { get; }
        public int MaxStake { get; }

        public VerifyDifficultyMessage(int difficulty, byte[] vrfBytes, int minStake, int maxStake)
        {
            Difficulty = difficulty;
            VrfBytes = vrfBytes;
            MinStake = minStake;
            MaxStake = maxStake;
        }
    }
}
