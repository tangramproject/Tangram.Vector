using System;
namespace Core.API.Consensus.Messages
{
    public class PrePrepare : IMessage, IEquatable<PrePrepare>
    {
        public string Hash { get; set; }
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public uint View { get; set; }

        public PrePrepare() { }

        public PrePrepare(string hash, ulong node, ulong round, uint view)
        {
            Hash = hash;
            Node = node;
            Round = round;
            View = view;
        }

        public MessageKind Kind()
        {
            return MessageKind.PrePrepareMsg;
        }

        public Tuple<ulong, ulong> NodeRound()
        {
            return Tuple.Create(Node, Round);
        }

        public override string ToString()
        {
            return $"pre-prepare{{node: {Node}, round: {Round}, view: {this.View}, hash: '{Util.FmtHash(Hash):S}'}}";
        }

        public bool Equals(PrePrepare other)
        {
            return other != null
                && other.Node == Node
                && other.Round == Round
                && other.Hash.Equals(Hash)
                && other.View == View;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, Node, Round, View);
        }
    }
}
