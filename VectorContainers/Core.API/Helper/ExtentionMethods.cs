using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Sodium;

namespace Core.API.Helper
{
    public static class ExtentionMethods
    {

        public static TResult IfNotNull<T, TResult>(this T target, Func<T, TResult> getValue) where T: class
        {
            return target == null ? default : getValue(target);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
                action(item);
        }

        public static byte[] ToBytes<T>(this T arg) => Encoding.UTF8.GetBytes(arg.ToString());

        public static string ToHex(this byte[] data) => Utilities.BinaryToHex(data);

        public static byte[] FromHex(this string hex) => Utilities.HexToBinary(hex);

        public static byte[] HexToBinary<T>(this T hex) => Utilities.HexToBinary(hex.ToString());

        public static string ToStr(this byte[] data) => Encoding.UTF8.GetString(data);

        public static void ExecuteInConstrainedRegion(this Action action)
        {
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                action();
            }
        }

        public static SecureString ToSecureString(this string value)
        {
            var secureString = new SecureString();
            Array.ForEach(value.ToArray(), secureString.AppendChar);
            secureString.MakeReadOnly();
            return secureString;
        }

        public static string ToUnSecureString(this SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
