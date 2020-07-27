// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMCore.Messages
{
    public class GracefulStopMessge
    {
        public byte[] Hash { get; }
        public TimeSpan TimeSpan { get; }
        public string Reason { get; }

        public GracefulStopMessge(byte[] hash, TimeSpan timeSpan, string reason)
        {
            Hash = hash;
            TimeSpan = timeSpan;
            Reason = reason;
        }
    }
}
