using Service.Services.WorkDay;
using System;

namespace Service.Helpers
{
    /// <summary>
    /// 海快報表最後傳輸日計算輔助類別。
    /// </summary>
    public static class SeaLastTransmitDateHelper
    {
        /// <summary>
        /// 依清關業者與到港日計算最後傳輸日。
        /// </summary>
        /// <param name="workDayService">工作天服務。</param>
        /// <param name="workDays">工作日清單。</param>
        /// <param name="holidays">假日清單。</param>
        /// <param name="modifyBy">清關業者。</param>
        /// <param name="eta">到港日。</param>
        /// <returns>最後傳輸日。</returns>
        public static DateTime GetLastTransmitDate(
            WorkDayService workDayService,
            DateTime[] workDays,
            DateTime[] holidays,
            string modifyBy,
            DateTime eta)
        {
            if (workDayService == null)
            {
                throw new ArgumentNullException(nameof(workDayService));
            }

            if (Contains(modifyBy, "郵聯"))
            {
                // 郵聯倉收費規則改為到港日後第 1 個工作日，不含到港日當天。
                return workDayService.AddWorkDays(workDays, holidays, eta, 1);
            }

            return eta.AddDays(6);
        }

        /// <summary>
        /// 判斷文字是否包含指定關鍵字。
        /// </summary>
        /// <param name="value">來源文字。</param>
        /// <param name="keyword">關鍵字。</param>
        /// <returns>包含關鍵字時回傳 true。</returns>
        private static bool Contains(string value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }
}
