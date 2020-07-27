// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Consensus.States
{
    public class PrePrepared : StateData
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public uint View { get; set; }

        public PrePrepared() { }

        public PrePrepared(ulong node, ulong round, uint view)
        {
            Node = node;
            Round = round;
            View = view;
        }

        public ulong GetRound()
        {
            return Round;
        }

        public StateDataKind SdKind()
        {
            return StateDataKind.PrePreparedState;
        }
    }
}
