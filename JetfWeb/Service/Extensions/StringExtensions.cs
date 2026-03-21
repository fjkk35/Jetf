using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 截斷字串到指定的最大長度
        /// </summary>
        /// <param name="value">要截斷的字串</param>
        /// <param name="maxLength">最大長度</param>
        /// <returns>截斷後的字串</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
