using System;
namespace Core.API.Consensus.States
{
    public class View : StateData, IEquatable<View>
    {
        public ulong Node { get; set; }
        public ulong Round { get; set; }

        public View() { }

        public View(ulong node, ulong round)
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
            return StateDataKind.ViewState;
        }

        public bool Equals(View other)
        {
            return other != null
                && other.Node == Node
                && other.Round == Round;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Node, Round);
        }
    }
}
