using System.Collections.Generic;

namespace Service.Services.ReconciliationIncludeTaxFormat.Domain
{
    /// <summary>
    /// 儲存包稅客戶 Excel 匯出格式的請求。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatSaveRequest
    {
        /// <summary>
        /// 格式識別碼；新增時為空白。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 格式名稱。
        /// </summary>
        public string FormatName { get; set; }

        /// <summary>
        /// 依畫面順序排列的欄位設定。
        /// </summary>
        public List<ReconciliationIncludeTaxFormatColumnRequest> Columns { get; set; }
    }
}
