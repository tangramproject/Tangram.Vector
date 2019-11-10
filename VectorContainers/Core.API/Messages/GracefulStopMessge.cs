using System;
namespace Core.API.Messages
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
