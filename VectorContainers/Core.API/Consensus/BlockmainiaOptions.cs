namespace Core.API.Consensus
{
    public class BlockmainiaOptions
    {
        public BlockmainiaOptions()
        {
            Microsecond *= Nanosecond;
            Millisecond *= Microsecond;
            Second *= Millisecond;
            Minute *= Second;
            Hour *= Minute;

            MB *= KB;
            GB *= MB;
            TB *= GB;

            BlockReferencesSizeLimit = 10 * MB;
            BlockTransactionsSizeLimit = 100 * MB;
            NonceExpiration = 30 * Second;
            RoundInterval = Nanosecond;
            ViewTimeout = 15;
            MaxPayload = 128 * MB;
            InitialBackoff = 1 * Second;
            MaxBackoff = 2 * Second;
            ReadTimeout = 10 * Second;
            WriteTimeout = 10 * Second;
            InitialRate = 10000;
            RateDecrease = 0.8F;
            RateIncrease = 1000;
            DriftTolerance = 10 * Millisecond;
            InitialWorkDuration = 100 * Millisecond;
        }

        public long Nanosecond { get; set; } = 1;
        public long Microsecond { get; set; } = 1000;
        public long Millisecond { get; set; } = 1000;
        public long Second { get; set; } = 1000;
        public long Minute { get; set; } = 60;
        public long Hour { get; set; } = 60;


        public int KB { get; set; } = 1024;
        public int MB { get; set; } = 1024;
        public long GB { get; set; } = 1024;
        public long TB { get; set; } = 1024;

        public long InitialBackoff { get; set; }
        public long MaxBackoff { get; set; }
        public long ReadTimeout { get; set; }
        public long WriteTimeout { get; set; }
        public int BlockReferencesSizeLimit { get; set; }
        public int BlockTransactionsSizeLimit { get; set; }
        public long NonceExpiration { get; set; }
        public long RoundInterval { get; set; }
        public int ViewTimeout { get; set; }
        public long MaxPayload { get; set; }
        public long DriftTolerance { get; set; }
        public long InitialWorkDuration { get; set; }
        public int InitialRate { get; set; }
        public float RateDecrease { get; set; }
        public int RateIncrease { get; set; }
    }
}
