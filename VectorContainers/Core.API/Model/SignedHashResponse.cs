using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.Models
{
    public class SignedHashResponse
    {
        public byte[] Signature { get; set; }
        public byte[] PublicKey { get; set; }
    }
}
