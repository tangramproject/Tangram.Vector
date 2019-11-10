using System;
namespace Core.API.Helper
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
