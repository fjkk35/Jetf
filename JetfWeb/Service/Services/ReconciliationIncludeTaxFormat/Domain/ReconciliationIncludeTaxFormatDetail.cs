using System.Collections.Generic;

namespace Service.Services.ReconciliationIncludeTaxFormat.Domain
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式明細。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatDetail
    {
        /// <summary>
        /// 格式識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 格式名稱。
        /// </summary>
        public string FormatName { get; set; }

        /// <summary>
        /// 欄位設定，依匯出順序排列。
        /// </summary>
        public List<ReconciliationIncludeTaxFormatColumnRequest> Columns { get; set; }
    }
}
