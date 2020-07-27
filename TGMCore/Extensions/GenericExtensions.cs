// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using Newtonsoft.Json;

namespace TGMCore.Extensions
{
    public static class GenericExtensions
    {
        public static bool IsDefault<T>(this T val)
        {
            return EqualityComparer<T>.Default.Equals(val, default);
        }

        public static T Cast<T>(this T val)
        {
            var json = JsonConvert.SerializeObject(val);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
