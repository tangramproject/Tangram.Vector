using System;
namespace Core.API.Consensus.Messages
{
    public class NewView : IMessage
    {
        public string Hash { get; set; }
        public ulong Node { get; set; }
        public ulong Round { get; set; }
        public ulong Sender { get; set; }
        public uint View { get; set; }

        public NewView() { }

        public NewView(string hash, ulong node, ulong round, ulong sender, uint view)
        {
            Hash = hash;
            Node = node;
            Round = round;
            Sender = sender;
            View = view;
        }

        public MessageKind Kind()
        {
            return MessageKind.NewViewMsg;
        }

        public Tuple<ulong, ulong> NodeRound()
        {
            return Tuple.Create(Node, Round);
        }

        public override string ToString()
        {
            return $"new-view{{node: {Node}, round: {Round}, view: {View}, hash: '{Util.FmtHash(Hash):S}', sender: {Sender}}}";
        }
    }
}
