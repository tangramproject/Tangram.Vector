// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Security;
using Sodium;

namespace TGMCore.Extentions
{
    public static class StringExtentions
    {
        public static SecureString ToSecureString(this string value)
        {
            var secureString = new SecureString();
            Array.ForEach(value.ToArray(), secureString.AppendChar);
            secureString.MakeReadOnly();
            return secureString;
        }

        public static byte[] FromHex(this string hex) => Utilities.HexToBinary(hex);
    }
}
