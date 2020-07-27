// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Text;

namespace TGMCore.Consensus.Messages
{
    public enum MessageKind
    {
        UnknownMsg,
        PrePrepareMsg,
        PrepareMsg,
        CommitMsg,
        ViewChangedMsg,
        NewViewMsg,
    }

    public interface IMessage
    {
        MessageKind Kind();
        Tuple<ulong, ulong> NodeRound();
        string ToString();
    }

    public static class Util
    {
        public static string GetMessageKindString(MessageKind m)
        {
            switch (m)
            {
                case MessageKind.CommitMsg:
                    return "commit";
                case MessageKind.NewViewMsg:
                    return "new-view";
                case MessageKind.PrepareMsg:
                    return "prepare";
                case MessageKind.PrePrepareMsg:
                    return "pre-prepare";
                case MessageKind.UnknownMsg:
                    return "unknown";
                case MessageKind.ViewChangedMsg:
                    return "view-change";
                default:
                    throw new Exception($"blockmania: unknown message kind: {m}");
            }
        }

        public static string FmtHash(string v)
        {
            if (v == "")
            {
                return "";
            }

            byte[] ba = Encoding.Default.GetBytes(v.Substring(6, 6));
            var hexString = BitConverter.ToString(ba);
            return hexString.Replace("-", "");
        }
    }

}
