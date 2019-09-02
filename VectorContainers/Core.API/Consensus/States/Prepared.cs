using System;
namespace Core.API.Consensus.States
{
    public class Prepared : StateData
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public uint View { get; set; }

        public Prepared() { }

        public Prepared(ulong node, ulong round, uint view)
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
            return StateDataKind.PreparedState;
        }
    }
}
