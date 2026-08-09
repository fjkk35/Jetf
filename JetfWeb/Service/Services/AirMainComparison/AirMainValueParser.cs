using System;
using System.Globalization;

namespace Service.Services.AirMainComparison
{
    /// <summary>
    /// 空運主號共用數值轉換工具。
    /// </summary>
    public static class AirMainValueParser
    {
        /// <summary>
        /// 將空白、千分位或小數格式安全轉為整數。
        /// </summary>
        /// <param name="value">來源字串。</param>
        /// <returns>轉換結果；無法轉換時為 0。</returns>
        public static int ParseInt(string value)
        {
            // 安全解析整數。
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var normalizedValue = value.Trim().Replace(",", "");
            int result;
            if (int.TryParse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            decimal decimalResult;
            if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimalResult))
            {
                return Convert.ToInt32(decimalResult);
            }

            return 0;
        }
    }
}
