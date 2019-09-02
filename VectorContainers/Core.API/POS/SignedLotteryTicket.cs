using System;
using System.Collections.Generic;
using System.Text;
using Core.API.LibSodium;
using Core.API.Models;

namespace Core.API.POS
{
    public class SignedLotteryTicket : SignedMessageBase
    {
        public LotteryTicket LotteryTicket { get; }
    }
}
