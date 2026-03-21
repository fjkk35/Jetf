using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Extensions
{
    public static class ConvertToNumberExtensions
    {
        public static int ToInt(this string value)
        {
            if (Int32.TryParse(value, out var num))
                return num;

            return 0;
        }

    }
}
