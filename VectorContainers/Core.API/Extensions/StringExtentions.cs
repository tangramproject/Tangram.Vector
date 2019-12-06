using System;
using System.Linq;
using System.Security;
using Sodium;

namespace Core.API.Extentions
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
