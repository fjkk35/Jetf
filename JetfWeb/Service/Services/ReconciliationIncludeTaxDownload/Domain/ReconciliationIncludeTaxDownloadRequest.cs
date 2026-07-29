using System.Collections.Generic;

namespace Service.Services.ReconciliationIncludeTaxDownload.Domain
{
    /// <summary>
    /// 包稅客戶明細下載查詢條件。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadRequest
    {
        /// <summary>
        /// 查詢開始日期文字。
        /// </summary>
        public string OutDateStart { get; set; }

        /// <summary>
        /// 查詢結束日期文字。
        /// </summary>
        public string OutDateEnd { get; set; }

        /// <summary>
        /// 選取的客戶代號；空集合表示全部客戶。
        /// </summary>
        public List<string> CustomerCodes { get; set; }

        /// <summary>
        /// 匯出格式識別碼。
        /// </summary>
        public int FormatId { get; set; }
    }
}
