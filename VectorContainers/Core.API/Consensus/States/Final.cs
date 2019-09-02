using System;
namespace Core.API.Consensus.States
{
    public class Final : StateData
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }

        public Final() { }

        public Final(ulong node, ulong round)
        {
            Node = node;
            Round = round;
        }

        public ulong GetRound()
        {
            return Round;
        }

        public StateDataKind SdKind()
        {
            return StateDataKind.FinalState;
        }
    }
}
