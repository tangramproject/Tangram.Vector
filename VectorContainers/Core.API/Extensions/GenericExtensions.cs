using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.API.Extensions
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
