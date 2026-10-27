using System.Collections.Generic;
using System.Globalization;

namespace Service.Services.FlightArrivalUpload
{
    /// <summary>
    /// 沿用原 DataTable 名稱比對規則，忽略大小寫、全半形、假名類型及尾端空白。
    /// </summary>
    internal sealed class FlightArrivalUploadNameComparer : IEqualityComparer<string>
    {
        private readonly CompareInfo _compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;

        /// <summary>
        /// 依原名稱比對規則判斷兩個客戶或派件公司名稱是否相同。
        /// </summary>
        /// <param name="x">第一個名稱。</param>
        /// <param name="y">第二個名稱。</param>
        /// <returns>名稱相同時為 true。</returns>
        public bool Equals(string x, string y)
        {
            return _compareInfo.Compare(
                (x ?? string.Empty).TrimEnd(' ', '\u3000'),
                (y ?? string.Empty).TrimEnd(' ', '\u3000'), Options) == 0;
        }

        /// <summary>
        /// 依相同名稱比對規則計算查找用的雜湊值。
        /// </summary>
        /// <param name="value">客戶或派件公司名稱。</param>
        /// <returns>忽略名稱差異後的雜湊值。</returns>
        public int GetHashCode(string value)
        {
            return _compareInfo.GetHashCode((value ?? string.Empty).TrimEnd(' ', '\u3000'), Options);
        }
    }
}
