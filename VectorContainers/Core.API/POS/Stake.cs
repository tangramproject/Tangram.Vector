using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.POS
{
    public class Stake
    {
        public ulong Balance { get; }
        
        public Stake(ulong balance)
        {
            Balance = balance;
        }
    }
}
