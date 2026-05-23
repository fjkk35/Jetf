using System;

namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示清關資料查詢後的中繼欄位。
    /// </summary>
    internal sealed class ClearanceLookupRow
    {
        /// <summary>
        /// 取得或設定資料來源代碼。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 取得或設定清關類型。
        /// </summary>
        public string ClearanceType { get; set; }

        /// <summary>
        /// 取得或設定清關單號。
        /// </summary>
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 取得或設定入倉時間。
        /// </summary>
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 取得或設定出倉時間。
        /// </summary>
        public DateTime? SignOutTime { get; set; }

        /// <summary>
        /// 取得或設定主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 取得或設定併袋號。
        /// </summary>
        public string MergeNumber { get; set; }
    }
}