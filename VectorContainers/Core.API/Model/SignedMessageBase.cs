using System;
using System.Diagnostics;
using System.Linq;
using Core.API.LibSodium;
using Core.API.Onion;
using Core.API.Signatures;
using Newtonsoft.Json;

namespace Core.API.Models
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
