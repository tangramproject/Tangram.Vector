// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMCore.Consensus.Messages
{
    public class Prepare : IMessage
    {
        public string Hash { get; set; }
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public ulong Sender { get; set; }
        public uint View { get; set; }

        public Prepare() { }

        public Prepare(string hash, ulong node, ulong round, ulong sender, uint view)
        {
            Hash = hash;
            Node = node;
            Round = round;
            Sender = sender;
            View = view;
        }

        public MessageKind Kind()
        {
            return MessageKind.PrepareMsg;
        }

        public Tuple<ulong, ulong> NodeRound()
        {
            return Tuple.Create(Node, Round);
        }

        public PrePrepare Pre()
        {
            return new PrePrepare(Hash, Node, Round, View);
        }

        public override string ToString()
        {
            return $"pre-prepare{{node: {Node}, round: {Round}, view: {View}, hash: '{Util.FmtHash(Hash):S}'}}";
        }
    }
}
