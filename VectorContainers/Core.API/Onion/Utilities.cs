using SimpleBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.API.Onion
{
    public static class Utilities
    {
        public static byte[] ConvertV3OnionHostnameToEd25518PublicKey(string hostname)
        {
            var hostnameBase32Encoded = hostname.ToUpperInvariant();

            byte[] decodedBase32 = Base32.Rfc4648.Decode(hostnameBase32Encoded).ToArray();
            byte[] ed25519_pk_bytes = new byte[32];

            Array.Copy(decodedBase32, ed25519_pk_bytes, ed25519_pk_bytes.Length);

            return ed25519_pk_bytes;
        }
    }
}
