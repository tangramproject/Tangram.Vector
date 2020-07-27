// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Diagnostics;
using System.Linq;
using TGMCore.LibSodium;
using TGMCore.Signatures;

namespace TGMCore.Models
{
    public class SignedMessageBase : ISignedMessageBase
    {
        public byte[] PublicKey { get; set; }
        public byte[] Hash { get; set; }
        public byte[] Signature { get; set; }

        public virtual bool IsValid()
        {
            throw new NotImplementedException();
        }

        public bool IsValid(string payload)
        {
            var hash = Cryptography.GenericHashNoKey(payload);

            if (!hash.SequenceEqual(Hash))
            {
                Debug.WriteLine("Hash check failed");
                return false;
            }

            var validSig = Ed25519.Verify(Signature, hash, PublicKey);

            if (!validSig)
            {
                Debug.WriteLine("Signature check failed");
                return false;
            }

            return true;
        }
    }
}
