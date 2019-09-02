using System;
namespace Core.API.Consensus.States
{
    public class Hnv : StateData
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public uint View { get; set; }

        public Hnv() { }

        public Hnv(ulong node, ulong round, uint view)
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
            return StateDataKind.HNVState;
        }
    }
}
