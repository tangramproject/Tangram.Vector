// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace TGMCore.Extentions
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

        public static ulong MulWithNaT(this ulong value) => value * 1000_000_000;

        public static double DivWithNaT(this ulong value) => Convert.ToDouble(value) / 1000_000_000;

        public static ulong ConvertToUInt64(this double value)
        {
            ulong amount;

            try
            {
                var parts = value.ToString().Split(new char[] { '.', ',' });
                var part1 = (ulong)Math.Truncate(value);

                if (parts.Length.Equals(1))
                    amount = part1.MulWithNaT();
                else
                {
                    var part2 = (ulong)((value - part1) * ulong.Parse("1".PadRight(parts[1].Length + 1, '0')) + 0.5);
                    amount = part1.MulWithNaT() + ulong.Parse(part2.ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return amount;
        }
    }
}
