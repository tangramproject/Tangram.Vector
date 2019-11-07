using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

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
    }
}
