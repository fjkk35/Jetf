using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Extensions
{
    public static class ConvertToDateTimeExtensions
    {
        public static DateTime? ToDateTime(this string value,string formatString)
        {
            if (DateTime.TryParseExact(value, formatString, null, System.Globalization.DateTimeStyles.None, out var date))
                return date;

            return null;

        }

        public static string ToDateTimeString(this string value, string formatString)
        {
            var formats = new[] {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(value, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var date))
            {
                return date.ToString(formatString);
            }

            return null;
        }

    }
}
