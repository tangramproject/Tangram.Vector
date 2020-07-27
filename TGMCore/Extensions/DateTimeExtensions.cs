// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMCore.Extentions
{
    public static class DateTimeExtensions
    {
        public static DateTime Truncate(this DateTime date, long resolution)
        {
            return new DateTime(date.Ticks - (date.Ticks % resolution), date.Kind);
        }

        public static DateTimeOffset Truncate(this DateTimeOffset date, long resolution)
        {
            return new DateTimeOffset(new DateTime(date.Ticks - (date.Ticks % resolution)));
        }
    }
}
