using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.POS
{
    public class LotteryTicketCommitment
    {
        public byte[] Signature { get; }
        public byte[] PublicKey { get; }

        public LotteryTicketCommitment(byte[] signature, byte[] publicKey)
        {
            Signature = signature;
            PublicKey = publicKey;
        }
    }
}
