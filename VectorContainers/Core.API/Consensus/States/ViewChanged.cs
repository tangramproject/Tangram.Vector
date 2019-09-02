using System;
namespace Core.API.Consensus.States
{
    public class ViewChanged : StateData
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public uint View { get; set; }

        public ViewChanged() { }

        public ViewChanged(ulong node, ulong round, uint view)
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
            return StateDataKind.ViewChangedState;
        }
    }
}
